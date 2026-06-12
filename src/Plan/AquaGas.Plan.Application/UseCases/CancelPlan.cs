using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class CancelPlan : ICancelPlan
{
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IContractPenaltyService _penaltyService;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;

    public CancelPlan(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        IContractPenaltyService penaltyService,
        IUserContextService userContext,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
        _penaltyRepository = penaltyRepository;
        _penaltyService = penaltyService;
        _userContext = userContext;
        _auditLogService = auditLogService;
    }

    public async Task<Result<CancelPlanResponse>> Execute(Guid planId, CancelPlanInput input)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan is null)
            return Result<CancelPlanResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled)
            return Result<CancelPlanResponse>.Fail(Error.Conflict("Plan is already canceled"));

        if (plan.Status == PlanStatus.Finished)
            return Result<CancelPlanResponse>.Fail(Error.Conflict("Cannot cancel a finished plan"));

        if (plan.Status != PlanStatus.Active &&
            plan.Status != PlanStatus.Suspended)
        {
            return Result<CancelPlanResponse>.Fail(
                Error.Conflict($"Cannot cancel a plan in {plan.Status} status"));
        }

        var penalties = await _penaltyRepository.GetByPlanIdAsync(planId);
        var hasOpenPenalty = penalties.Any(x =>
            x.Status == ContractPenaltyStatus.PendingPayment ||
            x.Status == ContractPenaltyStatus.Overdue);

        if (hasOpenPenalty)
            return Result<CancelPlanResponse>
                .Fail(Error.Conflict("Cannot cancel plan while there is an open penalty. Please pay or waive the penalty first."));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<CancelPlanResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<CancelPlanResponse>.Fail(userNameResult.Errors.ToArray());

        var deliveries = await _deliveryRepository.GetByPlanIdAsync(planId);
        var billings = await _billingRepository.GetByPlanIdAsync(planId);

        var pendingDeliveries = deliveries
            .Where(x => x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late)
            .ToList();

        if (pendingDeliveries.Any(x => x.DueDate.Date == DateTime.UtcNow.Date))
            return Result<CancelPlanResponse>.Fail(Error
                .Conflict("Cannot cancel plan with delivery scheduled for today. Please complete or reschedule the delivery first."));

        var previousStatus = plan.Status;

        foreach (var delivery in pendingDeliveries)
        {
            delivery.Cancel();
            _deliveryRepository.Update(delivery);
        }

        foreach (var billing in billings)
        {
            billing.MarkAsLate();
        }

        var penalty = _penaltyService.CalculateCancellationPenalty(
            plan,
            billings,
            userIdResult.Value!,
            input.Reason);

        var pendingBillings = billings
            .Where(x => x.Status == BillingStatus.Pending)
            .ToList();

        foreach (var billing in pendingBillings)
        {
            billing.Cancel();
            _billingRepository.Update(billing);
        }

        if (penalty != null && penalty.CalculatedAmount.Value > 0)
        {
            await _penaltyRepository.AddAsync(penalty);
        }

        plan.Cancel();
        _planRepository.Update(plan);

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "Plan",
            plan.Id,
            new { Status = previousStatus.ToString() },
            new
            {
                plan.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = plan.Status.ToString(),
                input.Reason,
                CanceledDeliveries = pendingDeliveries.Count,
                CanceledBillings = pendingBillings.Count,
                PenaltyGenerated = penalty != null,
                PenaltyAmount = penalty?.CalculatedAmount.Value
            });

        await _planRepository.SaveChangesAsync();
        await _deliveryRepository.SaveChangesAsync();
        await _billingRepository.SaveChangesAsync();
        await _penaltyRepository.SaveChangesAsync();

        PenaltyResponse? penaltyResponse = null;
        if (penalty != null && penalty.CalculatedAmount.Value > 0)
        {
            penaltyResponse = new PenaltyResponse
            {
                Id = penalty.Id,
                PlanId = penalty.PlanId,
                Type = penalty.Type,
                OriginalValue = penalty.OriginalValue.Value,
                RemainingValue = penalty.RemainingValue.Value,
                CalculatedAmount = penalty.CalculatedAmount.Value,
                Status = penalty.Status,
                Timestamp = penalty.Timestamp,
                DueDate = penalty.DueDate,
                PaidDate = penalty.PaidDate,
                PaidBy = penalty.PaidBy,
                WaivedBy = penalty.WaivedBy,
                WaivedAt = penalty.WaivedAt,
                WaiveReason = penalty.WaiveReason,
                CanceledBy = penalty.CanceledBy,
                CanceledAt = penalty.CanceledAt,
                CancelReason = penalty.CancelReason,
                Notes = penalty.Notes
            };
        }

        return Result<CancelPlanResponse>.Success(new CancelPlanResponse
        {
            PlanId = plan.Id,
            Status = plan.Status.ToString(),
            CanceledDeliveries = pendingDeliveries.Count,
            CanceledBillings = pendingBillings.Count,
            Penalty = penaltyResponse,
            Message = "Plan canceled successfully"
        });
    }
}

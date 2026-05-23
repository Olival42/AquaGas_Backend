using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class ReactivatePlan : IReactivatePlan
{
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IPlanDateService _dateService;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;

    public ReactivatePlan(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        IPlanDateService dateService,
        IUserContextService userContext,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
        _penaltyRepository = penaltyRepository;
        _dateService = dateService;
        _userContext = userContext;
        _auditLogService = auditLogService;
    }

    public async Task<Result<ReactivatePlanResponse>> Execute(Guid planId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan is null)
            return Result<ReactivatePlanResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status != PlanStatus.Suspended)
            return Result<ReactivatePlanResponse>
                .Fail(Error.Conflict($"Only suspended plans can be reactivated. Current status: {plan.Status}"));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<ReactivatePlanResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<ReactivatePlanResponse>.Fail(userNameResult.Errors.ToArray());

        var billings = await _billingRepository.GetByPlanIdAsync(planId);

        foreach (var billing in billings)
        {
            billing.MarkAsLate();
        }

        var hasLateBillings = billings.Any(x => x.Status == BillingStatus.Late);

        if (hasLateBillings)
            return Result<ReactivatePlanResponse>
                .Fail(Error.Conflict("Cannot reactivate plan while there are overdue billings. Please pay them first."));

        var penalties = await _penaltyRepository.GetByPlanIdAsync(planId);
        var hasOpenPenalty = penalties.Any(x => 
            x.Status == ContractPenaltyStatus.PendingPayment || 
            x.Status == ContractPenaltyStatus.Overdue);

        if (hasOpenPenalty)
            return Result<ReactivatePlanResponse>.Fail(
                Error.Conflict("Cannot reactivate plan while there is an open penalty. Please pay or waive the penalty first."));

        var previousStatus = plan.Status;
        plan.Reactivate();
        _planRepository.Update(plan);

        var deliveries = await _deliveryRepository.GetByPlanIdAsync(planId);

        foreach (var delivery in deliveries)
        {
            delivery.MarkAsLate();
        }

        var pendingDeliveries = deliveries
            .Where(x => x.Status == DeliveryStatus.Pending ||
                       x.Status == DeliveryStatus.Late ||
                       x.Status == DeliveryStatus.Cancelled)
            .OrderBy(x => x.Period)
            .ToList();

        var pendingBillings = billings
            .Where(x => x.Status == BillingStatus.Pending ||
                       x.Status == BillingStatus.Late ||
                       x.Status == BillingStatus.Cancelled)
            .OrderBy(x => x.DueDate)
            .ToList();

        if (!pendingDeliveries.Any() && !pendingBillings.Any())
            return Result<ReactivatePlanResponse>.Fail(
                Error.Conflict(
                    "Plan has no pending schedules to reactivate"));

        var now = DateTime.UtcNow;
        var nextDeliveryDate = now;

        for (int i = 0; i < pendingDeliveries.Count; i++)
        {
            var deliveryDate = GetNextValidDate(
                nextDeliveryDate,
                plan.DeliveryDay);

            pendingDeliveries[i].Reschedule(deliveryDate);
            _deliveryRepository.Update(pendingDeliveries[i]);

            nextDeliveryDate = deliveryDate.AddMonths(1);
        }

        var nextBillingDate = now;
        for (int i = 0; i < pendingBillings.Count; i++)
        {
            var billingDate = GetNextValidDate(
                nextBillingDate,
                plan.BillingDay);

            pendingBillings[i].Reschedule(billingDate);
            _billingRepository.Update(pendingBillings[i]);

            nextBillingDate = billingDate.AddMonths(1);
        }

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
                Status = plan.Status.ToString(),
                RescheduledDeliveries = pendingDeliveries.Count,
                RescheduledBillings = pendingBillings.Count
            });

        await _planRepository.SaveChangesAsync();
        await _deliveryRepository.SaveChangesAsync();
        await _billingRepository.SaveChangesAsync();

        return Result<ReactivatePlanResponse>.Success(
            new ReactivatePlanResponse
            {
                PlanId = plan.Id,
                Status = plan.Status.ToString(),
                RescheduledDeliveries = pendingDeliveries.Count,
                RescheduledBillings = pendingBillings.Count,
                Message = "Plan reactivated successfully"
            });
    }

    private DateTime AdvanceCycle(
        DateTime date)
    {
        return date.AddMonths(1);
    }

    private DateTime GetNextValidDate(
        DateTime baseDate,
        int day)
    {
        var adjusted = _dateService.AdjustDay(
            baseDate.Year,
            baseDate.Month,
            day);

        if (adjusted.Date < DateTime.UtcNow.Date)
        {
            var next = AdvanceCycle(baseDate);

            adjusted = _dateService.AdjustDay(
                next.Year,
                next.Month,
                day);
        }

        return adjusted;
    }
}

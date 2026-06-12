using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class SuspendPlan : ISuspendPlan
{
    private readonly IPlanRepository _planRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;

    public SuspendPlan(
        IPlanRepository planRepository,
        IUserContextService userContext,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
    }

    public async Task<Result<SuspendPlanResponse>> Execute(Guid id, SuspendPlanInput input)
    {
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
            return Result<SuspendPlanResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status != PlanStatus.Active)
            return Result<SuspendPlanResponse>
                .Fail(Error.Conflict($"Cannot suspend a plan in {plan.Status} status"));

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<SuspendPlanResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<SuspendPlanResponse>.Fail(userNameResult.Errors.ToArray());

        var previousStatus = plan.Status;
        plan.Suspend(input.Reason);

        var deliveries = await _deliveryRepository.GetByPlanIdAsync(plan.Id);
        var billings = await _billingRepository.GetByPlanIdAsync(plan.Id);

        foreach (var delivery in deliveries)
        {
            delivery.MarkAsLate();
        }

        foreach (var billing in billings)
        {
            billing.MarkAsLate();
        }

        var paidPeriods = billings
            .Where(x => x.Status == BillingStatus.Paid)
            .Select(x => x.Period)
            .ToList();

        var hasDeliveryToday =
            deliveries.Any(x =>
                x.Status == DeliveryStatus.Pending &&
                x.DueDate.Date == DateTime.UtcNow.Date);

        if (hasDeliveryToday)
            return Result<SuspendPlanResponse>.Fail(
                Error.Conflict(
                    "Cannot suspend plan with delivery scheduled for today"));

        var pendingDeliveries =
            deliveries.Where(x =>
                (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late) &&
                !paidPeriods.Contains(x.Period))
            .ToList();

        foreach (var delivery in pendingDeliveries)
        {
            delivery.Cancel();
            _deliveryRepository.Update(delivery);
        }

        var pendingBillings =
            billings.Where(x =>
                x.Status == BillingStatus.Pending ||
                x.Status == BillingStatus.Late)
            .ToList();

        foreach (var billing in pendingBillings)
        {
            billing.Cancel();
            _billingRepository.Update(billing);
        }

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
                Status = plan.Status.ToString(),
                input.Reason,
                CanceledDeliveries = pendingDeliveries.Count,
                CanceledBillings = pendingBillings.Count
            });

        await _deliveryRepository.SaveChangesAsync();
        await _billingRepository.SaveChangesAsync();
        await _planRepository.SaveChangesAsync();

        return Result<SuspendPlanResponse>.Success(new SuspendPlanResponse
        {
            PlanId = plan.Id,
            Status = plan.Status.ToString(),
            CanceledDeliveries = pendingDeliveries.Count,
            CanceledBillings = pendingBillings.Count,
            Reason = input.Reason,
            Message = "Plan suspended successfully"
        });
    }
}

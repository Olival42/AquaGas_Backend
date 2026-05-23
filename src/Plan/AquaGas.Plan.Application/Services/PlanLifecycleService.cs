using AquaGas.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;

namespace AquaGas.Plan.Application.Services;

public sealed class PlanLifecycleService : IPlanLifecycleService
{
    private const string AwaitingClosureReason =
        "Contract end date was reached, but there are still pending financial or operational items.";

    private const string FinishedReason =
        "All pending financial and operational items were resolved automatically.";

    private readonly IPlanRepository _planRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IAuditLogService _auditLogService;

    public PlanLifecycleService(
        IPlanRepository planRepository,
        IBillingRepository billingRepository,
        IDeliveryRepository deliveryRepository,
        IContractPenaltyRepository penaltyRepository,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _billingRepository = billingRepository;
        _deliveryRepository = deliveryRepository;
        _penaltyRepository = penaltyRepository;
        _auditLogService = auditLogService;
    }

    public async Task TryCompletePlanAsync(Guid planId)
    {
        var plan = await _planRepository.GetByIdAsync(planId);
        if (plan is null)
            return;

        if (plan.Status == PlanStatus.Canceled ||
            plan.Status == PlanStatus.Suspended ||
            plan.Status == PlanStatus.Finished)
            return;

        if (DateTime.UtcNow.Date < plan.EndDate.Date)
            return;

        var billings = await _billingRepository.GetByPlanIdAsync(planId);
        var deliveries = await _deliveryRepository.GetByPlanIdAsync(planId);
        var penalties = await _penaltyRepository.GetByPlanIdAsync(planId);

        var hasOpenBilling = billings.Any(x =>
            x.Status == BillingStatus.Pending ||
            x.Status == BillingStatus.Late);

        var hasOpenDelivery = deliveries.Any(x =>
            x.Status == DeliveryStatus.Pending ||
            x.Status == DeliveryStatus.Late);

        var hasOpenPenalty = penalties.Any(x =>
            x.Status == ContractPenaltyStatus.PendingPayment ||
            x.Status == ContractPenaltyStatus.Overdue);

        var allBillingsClosed = billings.All(x =>
            x.Status == BillingStatus.Paid ||
            x.Status == BillingStatus.Canceled);

        var allDeliveriesClosed = deliveries.All(x =>
            x.Status == DeliveryStatus.Delivered ||
            x.Status == DeliveryStatus.Canceled);

        var previousStatus = plan.Status;
        var previousFinishedAt = plan.FinishedAt;

        if (!hasOpenBilling &&
            !hasOpenDelivery &&
            !hasOpenPenalty &&
            allBillingsClosed &&
            allDeliveriesClosed)
        {
            plan.Finish();
        }
        else
        {
            plan.SetAwaitingClosure();
        }

        if (plan.Status == previousStatus)
            return;

        _planRepository.Update(plan);

        await _auditLogService.LogAsync(
            null,
            "System",
            AuditAction.UPDATE,
            "Plan",
            plan.Id,
            new
            {
                PreviousStatus = previousStatus.ToString(),
                FinishedAt = previousFinishedAt
            },
            new
            {
                NewStatus = plan.Status.ToString(),
                plan.FinishedAt,
                Reason = plan.Status == PlanStatus.Finished
                    ? FinishedReason
                    : AwaitingClosureReason,
                Timestamp = DateTime.UtcNow
            });

        await _planRepository.SaveChangesAsync();
    }
}

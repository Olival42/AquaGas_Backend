using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public sealed class DowngradePlan : IDowngradePlan
{
    private const int MaxOverdueDaysForDowngrade = 15;

    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserContextService _userContext;
    private readonly IPlanDateService _dateService;
    private readonly IPlanCalculationService _calculationService;
    private readonly IPlanBillingService _billingService;
    private readonly IAuditLogService _auditLogService;

    public DowngradePlan(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        IProductRepository productRepository,
        IUserContextService userContext,
        IPlanDateService dateService,
        IPlanCalculationService calculationService,
        IPlanBillingService billingService,
        IAuditLogService auditLogService)
    {
        _planRepository = planRepository;
        _deliveryRepository = deliveryRepository;
        _billingRepository = billingRepository;
        _penaltyRepository = penaltyRepository;
        _productRepository = productRepository;
        _userContext = userContext;
        _dateService = dateService;
        _calculationService = calculationService;
        _billingService = billingService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<DowngradePlanResponse>> Execute(Guid id, DowngradePlanInput input)
    {
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
            return Result<DowngradePlanResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled)
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade a canceled plan"));

        if (plan.Status == PlanStatus.Finished)
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade a finished plan"));

        if (plan.Status == PlanStatus.AwaitingClosure)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade a plan awaiting closure. Resolve pending financial or operational items first."));

        if (plan.Status == PlanStatus.Suspended)
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade a suspended plan. Reactivate it first."));

        var penalties = await _penaltyRepository.GetByPlanIdAsync(plan.Id);
        if (penalties.Any(x => x.Status == ContractPenaltyStatus.PendingPayment || x.Status == ContractPenaltyStatus.Overdue))
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade plan with pending or overdue penalties"));

        var lateBillings = await _billingRepository.GetLateByCustomerIdAsync(plan.CustomerId);
        if (lateBillings.Any(x => x.PlanId == plan.Id && x.DueDate < DateTime.UtcNow.AddDays(-MaxOverdueDaysForDowngrade)))
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade plan with billings overdue for more than 15 days"));

        var deliveries = await _deliveryRepository.GetByPlanIdAsync(plan.Id);
        var today = DateTime.UtcNow.Date;
       if (deliveries.Any(x => x.DueDate.Date == today && x.Status == DeliveryStatus.Pending))
            return Result<DowngradePlanResponse>.Fail(Error.Conflict("Cannot downgrade plan with a pending delivery scheduled for today"));

        var billings = await _billingRepository.GetByPlanIdAsync(plan.Id);
        var billingsByPeriod = billings.ToDictionary(x => x.Period);
        var lockedBillingPeriods = deliveries
            .Where(x => x.Status == DeliveryStatus.Delivered)
            .Where(delivery =>
                billingsByPeriod.TryGetValue(delivery.Period, out var billing) &&
                (billing.Status == BillingStatus.Pending || billing.Status == BillingStatus.Late))
            .Select(x => x.Period)
            .ToHashSet();

        var previousCycle = plan.Cycle;
        var previousTotal = plan.Total.Value;
        var previousEndDate = plan.EndDate;
        var previousTotalPrice = ClonePrice(plan.Total);
        var oldItemsSnapshot = plan.Items
            .Select(x => new { x.ProductId, Quantity = x.Quantity.Value })
            .ToList();
        var oldOpenFutureAmountValue = billings
            .Where(x => x.Status == BillingStatus.Pending || x.Status == BillingStatus.Late)
            .Sum(x => x.Amount.Value);

        var finalItems = plan.Items.ToDictionary(
            x => x.ProductId,
            x => x.Quantity.Value);

        int updatedItemsCount = 0;
        int removedItemsCount = 0;

        if (input.Items is not null)
        {
            foreach (var itemInput in input.Items)
            {
                if (!finalItems.TryGetValue(itemInput.ProductId, out var currentQuantity))
                    return Result<DowngradePlanResponse>.Fail(
                        Error.Conflict($"Cannot include new product {itemInput.ProductId} in downgrade endpoint"));

                if (itemInput.Quantity > currentQuantity)
                    return Result<DowngradePlanResponse>.Fail(
                        Error.Conflict($"Cannot increase quantity for product {itemInput.ProductId} in downgrade endpoint"));

                if (itemInput.Quantity == 0)
                {
                    finalItems.Remove(itemInput.ProductId);
                    removedItemsCount++;
                    continue;
                }

                if (itemInput.Quantity < currentQuantity)
                    updatedItemsCount++;

                finalItems[itemInput.ProductId] = itemInput.Quantity;
            }
        }

        if (finalItems.Count == 0)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan by removing all items. Cancel the plan instead."));

        decimal newMonthlySubtotal = 0;
        foreach (var (productId, quantity) in finalItems)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
                return Result<DowngradePlanResponse>.Fail(Error.NotFound($"Product {productId} not found"));

            newMonthlySubtotal += product.Price.Value * quantity;
        }

        var targetCycle = input.Cycle ?? plan.Cycle;
        var cycleChanged = input.Cycle.HasValue && input.Cycle.Value != plan.Cycle;
        if (targetCycle == PlanCycle.Custom &&
            cycleChanged &&
            (!input.DurationInMonths.HasValue || input.DurationInMonths.Value <= 0))
        {
            return Result<DowngradePlanResponse>.Fail(
                Error.Validation("Duration in months is required for a custom cycle"));
        }

        var currentTotalMonths = billings.Count;
        var durationInMonths = ResolveDurationInMonths(targetCycle, input.DurationInMonths, currentTotalMonths);

        DateTime newEndDate;
        int totalMonths;
        if (cycleChanged || input.DurationInMonths.HasValue)
        {
            newEndDate = _dateService.GenerateEndDate(
                plan.StartDate,
                targetCycle,
                plan.DeliveryDay,
                plan.BillingDay,
                durationInMonths);

            var tempBillings = _billingService.GenerateBillings(
                Guid.Empty,
                plan.StartDate,
                newEndDate,
                targetCycle,
                plan.BillingDay,
                Price.Create(1).Value!,
                durationInMonths);

            totalMonths = tempBillings.Count;
        }
        else
        {
            newEndDate = plan.EndDate;
            totalMonths = currentTotalMonths;
        }

        if (cycleChanged && totalMonths >= currentTotalMonths)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("The new cycle or duration must reduce the contract duration for a downgrade."));

        var paidBillingsList = billings
            .Where(x => x.Status == BillingStatus.Paid)
            .ToList();
        var paidBillingsCount = paidBillingsList.Count;
        var paidTotalAmount = paidBillingsList.Sum(x => x.Amount.Value);

        if (paidBillingsCount >= billings.Count)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan: all billings are already paid."));

        var highestDeliveredPeriod = deliveries
            .Where(x => x.Status == DeliveryStatus.Delivered)
            .Select(x => x.Period)
            .DefaultIfEmpty(0)
            .Max();
        var highestPaidPeriod = paidBillingsList
            .Select(x => x.Period)
            .DefaultIfEmpty(0)
            .Max();
        var highestConcludedPeriod = Math.Max(highestDeliveredPeriod, highestPaidPeriod);

        if (totalMonths < highestConcludedPeriod)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot reduce plan duration below periods that are already paid or delivered."));

        if (billings.Any(x =>
                x.Period > totalMonths &&
                x.Status != BillingStatus.Paid &&
                x.DueDate.Date <= today))
        {
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot reduce plan duration below periods that are already billed or overdue."));
        }

        if (deliveries.Any(x =>
                x.Period > totalMonths &&
                x.Status != DeliveryStatus.Delivered &&
                x.DueDate.Date <= today))
        {
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot reduce plan duration below periods that are already scheduled in the current timeline."));
        }

        var retainedFutureBillings = billings
            .Where(x =>
                x.Period <= totalMonths &&
                (x.Status == BillingStatus.Pending || x.Status == BillingStatus.Late) &&
                !lockedBillingPeriods.Contains(x.Period))
            .OrderBy(x => x.Period)
            .ToList();

        var lockedFutureBillings = billings
            .Where(x =>
                x.Period <= totalMonths &&
                (x.Status == BillingStatus.Pending || x.Status == BillingStatus.Late) &&
                lockedBillingPeriods.Contains(x.Period))
            .OrderBy(x => x.Period)
            .ToList();

        var retainedFutureDeliveries = deliveries
            .Where(x =>
                x.Period <= totalMonths &&
                (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late))
            .OrderBy(x => x.Period)
            .ThenBy(x => x.DueDate)
            .ToList();

        var cancelledFutureBillings = billings
            .Where(x =>
                x.Period > totalMonths &&
                (x.Status == BillingStatus.Pending || x.Status == BillingStatus.Late) &&
                !lockedBillingPeriods.Contains(x.Period))
            .OrderBy(x => x.Period)
            .ToList();

        var cancelledFutureDeliveries = deliveries
            .Where(x =>
                x.Period > totalMonths &&
                (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late))
            .OrderBy(x => x.Period)
            .ToList();

        var remainingMonths = retainedFutureBillings.Count;
        if (remainingMonths <= 0)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan: there are no future unpaid billings left to recalculate."));

        if (!HasConsistentPeriods(totalMonths, billings, deliveries))
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan because future billing and delivery periods are inconsistent."));

        var futureContractTotalResult = _calculationService.CalculateContractTotal(
            newMonthlySubtotal,
            remainingMonths,
            plan.CurrentDiscount);

        if (futureContractTotalResult.IsFailure)
            return Result<DowngradePlanResponse>.Fail(futureContractTotalResult.Errors.ToArray());

        var lockedUnpaidAmount = lockedFutureBillings.Sum(x => x.Amount.Value);
        var newTotalResult = Price.Create(
            paidTotalAmount +
            lockedUnpaidAmount +
            futureContractTotalResult.Value!.Value);
        if (newTotalResult.IsFailure)
            return Result<DowngradePlanResponse>.Fail(newTotalResult.Errors.ToArray());

        var newTotal = newTotalResult.Value!;
        if (newTotal.Value >= plan.Total.Value)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan because there is no real reduction in contract value."));

        var penaltyAmountValue = plan.Total.Value - newTotal.Value;
        if (penaltyAmountValue <= 0)
            return Result<DowngradePlanResponse>.Fail(
                Error.Conflict("Cannot downgrade plan because the generated penalty amount would be zero or negative."));

        foreach (var item in plan.Items.ToList())
        {
            if (!finalItems.TryGetValue(item.ProductId, out var targetQuantity))
            {
                plan.RemoveItem(item);
                continue;
            }

            if (targetQuantity != item.Quantity.Value)
            {
                var quantityResult = StockQuantity.Create(targetQuantity);
                if (quantityResult.IsFailure)
                    return Result<DowngradePlanResponse>.Fail(quantityResult.Errors.ToArray());

                item.UpdateQuantity(quantityResult.Value!);
            }
        }

        plan.Downgrade(targetCycle, newTotal, newEndDate);

        var monthlyValueResult = _calculationService.CalculateMonthlyBilling(
            futureContractTotalResult.Value!,
            remainingMonths);
        if (monthlyValueResult.IsFailure)
            return Result<DowngradePlanResponse>.Fail(monthlyValueResult.Errors.ToArray());

        var newMonthlyValue = monthlyValueResult.Value!;

        foreach (var billing in retainedFutureBillings)
        {
            billing.UpdateAmount(ClonePrice(newMonthlyValue));
        }

        int updatedDeliveriesCount = 0;
        int updatedBillingsCount = retainedFutureBillings.Count;
        int canceledDeliveriesCount = 0;
        int canceledBillingsCount = 0;

        var lastConcludedDelivery = deliveries
            .Where(x => x.Status == DeliveryStatus.Delivered)
            .OrderByDescending(x => x.DueDate)
            .FirstOrDefault();
        var currentDeliveryMonthDate = lastConcludedDelivery is not null
            ? lastConcludedDelivery.DueDate.AddMonths(1)
            : plan.StartDate;

        foreach (var delivery in retainedFutureDeliveries)
        {
            if (delivery.HasCustomSchedule)
            {
                currentDeliveryMonthDate = currentDeliveryMonthDate.AddMonths(1);
                continue;
            }

            var newDueDate = _dateService.AdjustDay(
                currentDeliveryMonthDate.Year,
                currentDeliveryMonthDate.Month,
                plan.DeliveryDay);
            delivery.Reschedule(newDueDate);
            currentDeliveryMonthDate = currentDeliveryMonthDate.AddMonths(1);
            updatedDeliveriesCount++;
        }

        foreach (var delivery in cancelledFutureDeliveries)
        {
            delivery.Cancel();
            _deliveryRepository.Update(delivery);
            canceledDeliveriesCount++;
        }

        var lastPaidBilling = billings
            .Where(x => x.Status == BillingStatus.Paid)
            .OrderByDescending(x => x.DueDate)
            .FirstOrDefault();
        var currentBillingMonthDate = lastPaidBilling is not null
            ? lastPaidBilling.DueDate.AddMonths(1)
            : plan.StartDate;

        foreach (var billing in retainedFutureBillings)
        {
            var newDueDate = _dateService.AdjustDay(
                currentBillingMonthDate.Year,
                currentBillingMonthDate.Month,
                plan.BillingDay);
            billing.Reschedule(newDueDate);
            currentBillingMonthDate = currentBillingMonthDate.AddMonths(1);
        }

        foreach (var billing in cancelledFutureBillings)
        {
            billing.Cancel();
            _billingRepository.Update(billing);
            canceledBillingsCount++;
        }

        var penaltyAmount = Price.Create(penaltyAmountValue).Value!;
        var remainingFutureAmount = Price.Create(oldOpenFutureAmountValue).Value!;

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<DowngradePlanResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<DowngradePlanResponse>.Fail(userNameResult.Errors.ToArray());

        var penalty = new ContractPenalty(
            plan.Id,
            plan.CustomerId,
            userIdResult.Value!,
            ContractPenaltyType.Downgrade,
            previousTotalPrice,
            remainingFutureAmount,
            penaltyAmount,
            input.Reason);

        await _penaltyRepository.AddAsync(penalty);

        _planRepository.Update(plan);

        var newItemsSnapshot = plan.Items
            .Select(x => new { x.ProductId, Quantity = x.Quantity.Value })
            .ToList();

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "Plan",
            plan.Id,
            new
            {
                Cycle = previousCycle.ToString(),
                Total = previousTotal,
                EndDate = previousEndDate,
                Items = oldItemsSnapshot
            },
            new
            {
                Cycle = plan.Cycle.ToString(),
                Total = plan.Total.Value,
                plan.EndDate,
                Items = newItemsSnapshot,
                RequestedItems = input.Items,
                RequestedCycle = input.Cycle?.ToString(),
                UpdatedBillings = updatedBillingsCount,
                UpdatedDeliveries = updatedDeliveriesCount,
                CanceledBillings = canceledBillingsCount,
                CanceledDeliveries = canceledDeliveriesCount,
                UpdatedItems = updatedItemsCount,
                RemovedItems = removedItemsCount,
                PenaltyAmount = penalty.CalculatedAmount.Value,
                PenaltyId = penalty.Id,
                input.Reason
            });

        await _planRepository.SaveChangesAsync();
        await _deliveryRepository.SaveChangesAsync();
        await _billingRepository.SaveChangesAsync();
        await _penaltyRepository.SaveChangesAsync();

        var summary =
            $"Downgrade applied with financial reduction of {penaltyAmount.Value:F2}. " +
            $"{updatedItemsCount} item(s) reduced, {removedItemsCount} item(s) removed, " +
            $"{updatedBillingsCount} future billing(s) updated, {updatedDeliveriesCount} future delivery(s) updated, " +
            $"{canceledBillingsCount} future billing(s) canceled and {canceledDeliveriesCount} future delivery(s) canceled.";

        return Result<DowngradePlanResponse>.Success(new DowngradePlanResponse
        {
            PlanId = plan.Id,
            PreviousTotal = previousTotal,
            NewTotal = plan.Total.Value,
            PreviousCycle = previousCycle,
            Cycle = plan.Cycle,
            UpdatedBillings = updatedBillingsCount,
            UpdatedDeliveries = updatedDeliveriesCount,
            CanceledBillings = canceledBillingsCount,
            CanceledDeliveries = canceledDeliveriesCount,
            UpdatedItems = updatedItemsCount,
            RemovedItems = removedItemsCount,
            Penalty = MapPenalty(penalty),
            Summary = summary,
            Message = "Plan downgraded successfully"
        });
    }

    private static int? ResolveDurationInMonths(
        PlanCycle targetCycle,
        int? inputDuration,
        int existingBillingCount)
    {
        if (targetCycle != PlanCycle.Custom)
            return null;

        if (inputDuration.HasValue)
            return inputDuration;

        return existingBillingCount > 0 ? existingBillingCount : 1;
    }

    private static bool HasConsistentPeriods(
        int totalMonths,
        List<Billing> billings,
        List<Delivery> deliveries)
    {
        var billingPeriods = billings
            .Where(x => x.Period <= totalMonths)
            .Select(x => x.Period)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var deliveryPeriods = deliveries
            .Where(x => x.Period <= totalMonths)
            .Select(x => x.Period)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return billingPeriods.SequenceEqual(Enumerable.Range(1, totalMonths)) &&
               deliveryPeriods.SequenceEqual(Enumerable.Range(1, totalMonths));
    }

    private static PenaltyResponse MapPenalty(ContractPenalty penalty)
    {
        return new PenaltyResponse
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

    private static Price ClonePrice(Price price)
        => Price.Create(price.Value).Value!;
}

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

public sealed class UpgradePlan : IUpgradePlan
{
    private readonly IPlanRepository _planRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IContractPenaltyRepository _penaltyRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserContextService _userContext;
    private readonly IPlanDateService _dateService;
    private readonly IPlanCalculationService _calculationService;
    private readonly IPlanDeliveryService _deliveryService;
    private readonly IPlanBillingService _billingService;
    private readonly IAuditLogService _auditLogService;

    public UpgradePlan(
        IPlanRepository planRepository,
        IDeliveryRepository deliveryRepository,
        IBillingRepository billingRepository,
        IContractPenaltyRepository penaltyRepository,
        IProductRepository productRepository,
        IUserContextService userContext,
        IPlanDateService dateService,
        IPlanCalculationService calculationService,
        IPlanDeliveryService deliveryService,
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
        _deliveryService = deliveryService;
        _billingService = billingService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<UpgradePlanResponse>> Execute(Guid id, UpgradePlanInput input)
    {
        if (input.Cycle is null &&
            (input.Items is null || !input.Items.Any()))
        {
            return Result<UpgradePlanResponse>.Fail(
                Error.Validation("At least one change must be informed."));
        }

        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
            return Result<UpgradePlanResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled)
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade a canceled plan"));

        if (plan.Status == PlanStatus.Finished)
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade a finished plan"));

        if (plan.Status == PlanStatus.AwaitingClosure)
            return Result<UpgradePlanResponse>.Fail(
                Error.Conflict("Cannot upgrade a plan awaiting closure. Resolve pending financial or operational items first."));

        if (plan.Status == PlanStatus.Suspended)
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade a suspended plan. Reactivate it first."));

        var penalties = await _penaltyRepository.GetByPlanIdAsync(plan.Id);
        if (penalties.Any(x => x.Status == ContractPenaltyStatus.PendingPayment || x.Status == ContractPenaltyStatus.Overdue))
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade plan with pending or overdue penalties"));

        var lateBillings = await _billingRepository.GetLateByCustomerIdAsync(plan.CustomerId);
        if (lateBillings.Any(x => x.PlanId == plan.Id && x.DueDate < DateTime.UtcNow.AddDays(-15)))
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade plan with billings overdue for more than 15 days"));

        var deliveries = await _deliveryRepository.GetByPlanIdAsync(plan.Id);
        var today = DateTime.UtcNow.Date;
        if (deliveries.Any(x => x.DueDate.Date == today && x.Status == DeliveryStatus.Pending))
            return Result<UpgradePlanResponse>.Fail(Error.Conflict("Cannot upgrade plan with a pending delivery scheduled for today"));

        var billings = await _billingRepository.GetByPlanIdAsync(plan.Id);

        var billingsByPeriod = billings.ToDictionary(x => x.Period);
        var hasDeliveredWithUnpaidBilling = deliveries
            .Where(x => x.Status == DeliveryStatus.Delivered)
            .Any(delivery =>
                billingsByPeriod.TryGetValue(delivery.Period, out var billing) &&
                (billing.Status == BillingStatus.Pending || billing.Status == BillingStatus.Late));

        if (hasDeliveredWithUnpaidBilling)
            return Result<UpgradePlanResponse>.Fail(
                Error.Conflict(
                    "Cannot upgrade plan while a delivered period has pending or overdue billing."));

        var oldItemsSnapshot = plan.Items
            .Select(x => new { x.ProductId, Quantity = x.Quantity.Value })
            .ToList();

        var finalItems = plan.Items.ToDictionary(
            x => x.ProductId,
            x => x.Quantity.Value);

        if (input.Items is not null)
        {
            foreach (var itemInput in input.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemInput.ProductId);
                if (product is null)
                    return Result<UpgradePlanResponse>.Fail(Error.NotFound($"Product {itemInput.ProductId} not found"));

                if (!product.IsActive)
                    return Result<UpgradePlanResponse>.Fail(Error.Conflict($"Product {product.Name.Value} is not active"));

                if (finalItems.TryGetValue(itemInput.ProductId, out var currentQuantity) &&
                    itemInput.Quantity < currentQuantity)
                {
                    return Result<UpgradePlanResponse>.Fail(
                        Error.Conflict($"Downgrade of quantity for product {product.Name.Value} is not allowed in this endpoint"));
                }

                finalItems[itemInput.ProductId] = itemInput.Quantity;
            }
        }

        decimal newMonthlySubtotal = 0;

        foreach (var (productId, quantity) in finalItems)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
                return Result<UpgradePlanResponse>.Fail(Error.NotFound($"Product {productId} not found"));

            if (!product.IsActive)
                return Result<UpgradePlanResponse>.Fail(Error.Conflict($"Product {product.Name.Value} is not active"));

            newMonthlySubtotal += product.Price.Value * quantity;
        }

        var targetCycle = input.Cycle ?? plan.Cycle;
        var cycleChanged = input.Cycle.HasValue && input.Cycle.Value != plan.Cycle;

        if (targetCycle == PlanCycle.Custom &&
            cycleChanged &&
            (!input.DurationInMonths.HasValue || input.DurationInMonths.Value <= 0))
        {
            return Result<UpgradePlanResponse>.Fail(
                Error.Validation("Duration in months is required for a custom cycle"));
        }

        var durationInMonths = ResolveDurationInMonths(targetCycle, input.DurationInMonths, billings.Count);

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
            totalMonths = billings.Count;
        }

        if (totalMonths < billings.Count)
        {
            return Result<UpgradePlanResponse>.Fail(
                Error.Conflict($"The new plan duration ({totalMonths} months) cannot be less than the current plan duration ({billings.Count} months)"));
        }

        var paidBillingsList = billings
            .Where(x => x.Status == BillingStatus.Paid)
            .ToList();
        var paidBillingsCount = paidBillingsList.Count;
        var paidTotalAmount = paidBillingsList.Sum(x => x.Amount.Value);
        var remainingMonths = totalMonths - paidBillingsCount;

        if (remainingMonths <= 0)
            return Result<UpgradePlanResponse>.Fail(
                Error.Conflict("Cannot upgrade plan: all billings are already paid."));

        var futureContractTotalResult = _calculationService.CalculateContractTotal(
            newMonthlySubtotal,
            remainingMonths,
            plan.CurrentDiscount);

        if (futureContractTotalResult.IsFailure)
            return Result<UpgradePlanResponse>.Fail(futureContractTotalResult.Errors.ToArray());

        var newTotalResult = Price.Create(paidTotalAmount + futureContractTotalResult.Value!.Value);
        if (newTotalResult.IsFailure)
            return Result<UpgradePlanResponse>.Fail(newTotalResult.Errors.ToArray());

        var newTotal = newTotalResult.Value!;
        if (newTotal.Value <= plan.Total.Value)
            return Result<UpgradePlanResponse>.Fail(
                Error.Conflict("New total must be greater than the current total for an upgrade"));

        var previousCycle = plan.Cycle;
        var previousTotal = plan.Total.Value;

        if (input.Items is not null)
        {
            foreach (var itemInput in input.Items)
            {
                var quantityResult = StockQuantity.Create(itemInput.Quantity);
                if (quantityResult.IsFailure)
                    return Result<UpgradePlanResponse>.Fail(quantityResult.Errors.ToArray());

                var existingItem = plan.Items.FirstOrDefault(x => x.ProductId == itemInput.ProductId);
                if (existingItem is not null)
                {
                    existingItem.UpdateQuantity(quantityResult.Value!);
                }
                else
                {
                    plan.AddItem(new PlanItem(plan.Id, itemInput.ProductId, quantityResult.Value!));
                }
            }
        }

        plan.Upgrade(targetCycle, newTotal, newEndDate);

        var monthlyValueResult = _calculationService.CalculateMonthlyBilling(
            futureContractTotalResult.Value!,
            remainingMonths);

        if (monthlyValueResult.IsFailure)
            return Result<UpgradePlanResponse>.Fail(monthlyValueResult.Errors.ToArray());

        var newMonthlyValue = monthlyValueResult.Value!;
        var futureBillings = billings
            .Where(x => x.Status == BillingStatus.Pending || x.Status == BillingStatus.Late)
            .ToList();

        foreach (var billing in futureBillings)
        {
            billing.UpdateAmount(ClonePrice(newMonthlyValue));
        }

        int updatedDeliveriesCount = 0;
        int updatedBillingsCount = futureBillings.Count;

        if (cycleChanged || totalMonths > billings.Count)
        {
            var futureDeliveries = deliveries
                .Where(x => x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late)
                .OrderBy(x => x.Period)
                .ThenBy(x => x.DueDate)
                .ToList();
            var lastConcludedDelivery = deliveries
                .Where(x => x.Status == DeliveryStatus.Delivered)
                .OrderByDescending(x => x.DueDate)
                .FirstOrDefault();
            var highestExistingDeliveryPeriod = deliveries.Count != 0
                ? deliveries.Max(x => x.Period)
                : 0;

            var currentMonthDate = lastConcludedDelivery is not null
                ? lastConcludedDelivery.DueDate.AddMonths(1)
                : plan.StartDate;

            foreach (var delivery in futureDeliveries)
            {
                if (delivery.HasCustomSchedule)
                {
                    currentMonthDate = currentMonthDate.AddMonths(1);
                    continue;
                }

                var newDueDate = _dateService.AdjustDay(
                    currentMonthDate.Year,
                    currentMonthDate.Month,
                    plan.DeliveryDay);
                delivery.Reschedule(newDueDate);
                currentMonthDate = currentMonthDate.AddMonths(1);
                updatedDeliveriesCount++;
            }

            while (highestExistingDeliveryPeriod < totalMonths)
            {
                highestExistingDeliveryPeriod++;
                var newDueDate = _dateService.AdjustDay(
                    currentMonthDate.Year,
                    currentMonthDate.Month,
                    plan.DeliveryDay);
                var newDelivery = new Delivery(
                    plan.Id,
                    highestExistingDeliveryPeriod,
                    newDueDate);
                await _deliveryRepository.AddAsync(newDelivery);
                currentMonthDate = currentMonthDate.AddMonths(1);
                updatedDeliveriesCount++;
            }

            var lastPaidBilling = billings
                .Where(x => x.Status == BillingStatus.Paid)
                .OrderByDescending(x => x.DueDate)
                .FirstOrDefault();

            var currentBillingMonthDate = lastPaidBilling is not null
                ? lastPaidBilling.DueDate.AddMonths(1)
                : plan.StartDate;

            foreach (var billing in futureBillings)
            {
                var newDueDate = _dateService.AdjustDay(
                    currentBillingMonthDate.Year,
                    currentBillingMonthDate.Month,
                    plan.BillingDay);
                billing.Reschedule(newDueDate);
                currentBillingMonthDate = currentBillingMonthDate.AddMonths(1);
            }

            while (updatedBillingsCount + paidBillingsCount < totalMonths)
            {
                var newDueDate = _dateService.AdjustDay(
                    currentBillingMonthDate.Year,
                    currentBillingMonthDate.Month,
                    plan.BillingDay);
                var newBilling = new Billing(
                    plan.Id,
                    updatedBillingsCount + paidBillingsCount + 1,
                    newDueDate,
                    ClonePrice(newMonthlyValue));
                await _billingRepository.AddAsync(newBilling);
                currentBillingMonthDate = currentBillingMonthDate.AddMonths(1);
                updatedBillingsCount++;
            }
        }

        var userIdResult = _userContext.GetUserId();
        var userNameResult = _userContext.GetUserName();

        if (userIdResult.IsFailure)
            return Result<UpgradePlanResponse>.Fail(userIdResult.Errors.ToArray());

        if (userNameResult.IsFailure)
            return Result<UpgradePlanResponse>.Fail(userNameResult.Errors.ToArray());

        var newItemsSnapshot = plan.Items
            .Select(x => new { x.ProductId, Quantity = x.Quantity.Value })
            .ToList();

        await _auditLogService.LogAsync(
            userIdResult.Value,
            userNameResult.Value ?? "System",
            AuditAction.UPDATE,
            "Plan",
            plan.Id,
            new
            {
                Cycle = previousCycle.ToString(),
                Total = previousTotal,
                Items = oldItemsSnapshot
            },
            new
            {
                Cycle = plan.Cycle.ToString(),
                Total = plan.Total.Value,
                Items = newItemsSnapshot,
                RequestedItems = input.Items,
                RequestedCycle = input.Cycle?.ToString(),
                UpdatedBillings = updatedBillingsCount,
                UpdatedDeliveries = updatedDeliveriesCount,
                Reason = input.Reason
            });

        await _planRepository.SaveChangesAsync();

        return Result<UpgradePlanResponse>.Success(new UpgradePlanResponse
        {
            PlanId = plan.Id,
            PreviousTotal = previousTotal,
            NewTotal = plan.Total.Value,
            PreviousCycle = previousCycle,
            Cycle = plan.Cycle,
            UpdatedBillings = updatedBillingsCount,
            UpdatedDeliveries = updatedDeliveriesCount,
            UpdatedItems = input.Items?.Count ?? 0,
            Message = "Plan upgraded successfully"
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

    private static Price ClonePrice(Price price)
        => Price.Create(price.Value).Value!;
}

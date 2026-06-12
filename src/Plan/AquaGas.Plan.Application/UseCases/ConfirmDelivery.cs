using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;

namespace AquaGas.Plan.Application.UseCases;

public sealed class ConfirmDelivery : IConfirmDelivery
{
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUserContextService _userContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanLifecycleService _planLifecycleService;

    public ConfirmDelivery(
        IDeliveryRepository deliveryRepository,
        IPlanRepository planRepository,
        IBillingRepository billingRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IUserContextService userContext,
        IAuditLogService auditLogService,
        IPlanLifecycleService planLifecycleService)
    {
        _deliveryRepository = deliveryRepository;
        _planRepository = planRepository;
        _billingRepository = billingRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _userContext = userContext;
        _auditLogService = auditLogService;
        _planLifecycleService = planLifecycleService;
    }

    public async Task<Result<ConfirmDeliveryResponse>> Execute(ConfirmDeliveryInput input)
    {
        var userIdResult = _userContext.GetUserId();
        if (userIdResult.IsFailure)
            return Result<ConfirmDeliveryResponse>.Fail(userIdResult.Errors.ToArray());

        var currentUserName = _userContext.GetUserName();
        if (currentUserName.IsFailure)
            return Result<ConfirmDeliveryResponse>.Fail(currentUserName.Errors.ToArray());

        var delivery = await _deliveryRepository.GetByIdAsync(input.DeliveryId);
        if (delivery is null)
            return Result<ConfirmDeliveryResponse>.Fail(Error.NotFound("Delivery not found"));

        if (delivery.Status == DeliveryStatus.Delivered)
            return Result<ConfirmDeliveryResponse>.Fail(Error.Conflict("Delivery already completed"));

        if (delivery.Status == DeliveryStatus.Canceled)
            return Result<ConfirmDeliveryResponse>.Fail(Error.Conflict("Cannot confirm a canceled delivery"));

        if (DateTime.UtcNow.Date < delivery.DueDate.Date)
            return Result<ConfirmDeliveryResponse>.Fail(Error.Conflict(
                $"Cannot confirm delivery before the scheduled date ({delivery.DueDate:dd/MM/yyyy})"));

        var plan = await _planRepository.GetByIdAsync(delivery.PlanId);
        if (plan is null)
            return Result<ConfirmDeliveryResponse>.Fail(Error.NotFound("Plan not found"));

        if (plan.Status == PlanStatus.Canceled)
            return Result<ConfirmDeliveryResponse>
                .Fail(Error.Conflict(
                    "Cannot confirm delivery for a canceled plan"));

        if (plan.Status == PlanStatus.Suspended)
            return Result<ConfirmDeliveryResponse>
                .Fail(Error.Conflict(
                    "Cannot confirm delivery for a suspended plan"));

        var allBillings = await _billingRepository.GetByPlanIdAsync(plan.Id);
        var overdueBilling = allBillings
            .FirstOrDefault(x => (x.Status == BillingStatus.Late ||
                                 (x.Status == BillingStatus.Pending && DateTime.UtcNow > x.DueDate)));

        if (overdueBilling is not null)
            return Result<ConfirmDeliveryResponse>.Fail(Error.Conflict(
                $"Cannot confirm delivery because there is an overdue billing (Due Date: {overdueBilling.DueDate:dd/MM/yyyy})"));

        var allDeliveries = await _deliveryRepository.GetByPlanIdAsync(plan.Id);

        foreach (var item in allDeliveries
             .Where(x => x.Status == DeliveryStatus.Pending))
        {
            item.MarkAsLate();
            _deliveryRepository.Update(item);
        }

        var previousPending = allDeliveries
            .Where(x => x.Period < delivery.Period &&
                       (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Late))
            .OrderBy(x => x.Period)
            .FirstOrDefault();

        if (previousPending is not null)
            return Result<ConfirmDeliveryResponse>.Fail(Error.Conflict(
                $"Cannot confirm period {delivery.Period} because period {previousPending.Period} is still pending or late."));

        var products = new Dictionary<Guid, ProductEntity>();
        foreach (var item in plan.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product is null)
                return Result<ConfirmDeliveryResponse>.Fail(Error.NotFound($"Product {item.ProductId} not found"));

            if (product.Quantity.Value < item.Quantity.Value)
                return Result<ConfirmDeliveryResponse>.Fail(Error.InsufficientStockForProduct(
                    product.Name.Value, product.Id, product.Quantity.Value, item.Quantity.Value));

            products[item.ProductId] = product;
        }

        foreach (var item in plan.Items)
        {
            var product = products[item.ProductId];

            var decreaseResult = product.DecreaseStock(item.Quantity.Value);
            if (decreaseResult.IsFailure)
                return Result<ConfirmDeliveryResponse>.Fail(decreaseResult.Errors.ToArray());

            _productRepository.Update(product);

            var movement = StockMovement.Create(
                product.Id,
                StockMovementType.Exit,
                item.Quantity.Value,
                "Plan delivery",
                userIdResult.Value!,
                delivery.Id);

            await _stockMovementRepository.AddAsync(movement);
        }

        delivery.Complete();
        _deliveryRepository.Update(delivery);

        await _productRepository.SaveChangesAsync();
        await _deliveryRepository.SaveChangesAsync();
        await _planLifecycleService.TryCompletePlanAsync(plan.Id);

        await _auditLogService.LogAsync(
            userIdResult.Value!,
            currentUserName.Value,
            AuditAction.UPDATE,
            "Delivery",
            delivery.Id,
            new
            {
                PreviousStatus = DeliveryStatus.Pending.ToString()
            },
            new
            {
                delivery.Id,
                delivery.PlanId,
                NewStatus = delivery.Status.ToString(),
                delivery.DeliveryDate
            });

        return Result<ConfirmDeliveryResponse>.Success(new ConfirmDeliveryResponse
        {
            DeliveryId = delivery.Id,
            DeliveryDate = delivery.DeliveryDate,
            Status = delivery.Status.ToString(),
            Message = "Delivery confirmed successfully"
        });
    }
}

using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class DowngradePlanTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IPlanDateService> _dateService = new();
    private readonly Mock<IPlanCalculationService> _calc = new();
    private readonly Mock<IPlanBillingService> _billingService = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private readonly DowngradePlan _useCase;

    public DowngradePlanTests()
    {
        _useCase = new DowngradePlan(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _productRepo.Object,
            _userContext.Object,
            _dateService.Object,
            _calc.Object,
            _billingService.Object,
            _audit.Object);
    }

    private static PlanEntity CreatePlan(
        PlanStatus status = PlanStatus.Active,
        DateTime? endDate = null)
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(1000).Value!,
            DateTime.UtcNow.AddDays(-40),
            endDate ?? DateTime.UtcNow.AddDays(50),
            10,
            10,
            null);

        switch (status)
        {
            case PlanStatus.Canceled:
                plan.Cancel();
                break;
            case PlanStatus.Finished:
                plan.Finish();
                break;
            case PlanStatus.Suspended:
                plan.Suspend("test");
                break;
            case PlanStatus.AwaitingClosure:
                plan.SetAwaitingClosure();
                break;
        }

        return plan;
    }

    private static ProductEntity CreateProduct(
        string name = "Produto Teste",
        decimal price = 100m,
        int quantity = 10,
        bool active = true)
    {
        var product = new ProductEntity(
            ProductName.Create(name).Value!,
            TypeProduct.Water,
            Price.Create(price).Value!,
            StockQuantity.Create(quantity).Value!);

        if (!active)
        {
            product = new ProductEntity(
                ProductName.Create(name).Value!,
                TypeProduct.Water,
                Price.Create(price).Value!,
                StockQuantity.Create(0).Value!);
            product.Deactivate();
        }

        return product;
    }

    private static Billing CreateBilling(
        Guid planId,
        int period,
        BillingStatus status,
        DateTime dueDate,
        decimal amount = 100m)
    {
        var billing = new Billing(
            planId,
            period,
            dueDate,
            Price.Create(amount).Value!);

        switch (status)
        {
            case BillingStatus.Paid:
                billing.Pay(Guid.NewGuid());
                break;
            case BillingStatus.Canceled:
                billing.Cancel();
                break;
            case BillingStatus.Late:
                billing = new Billing(
                    planId,
                    period,
                    DateTime.UtcNow.AddDays(-20),
                    Price.Create(amount).Value!);
                billing.MarkAsLate();
                break;
        }

        return billing;
    }

    private static Delivery CreateDelivery(
        Guid planId,
        int period,
        DeliveryStatus status,
        DateTime dueDate)
    {
        var delivery = new Delivery(
            planId,
            period,
            dueDate);

        switch (status)
        {
            case DeliveryStatus.Delivered:
                delivery.Complete();
                break;
            case DeliveryStatus.Canceled:
                delivery.Cancel();
                break;
            case DeliveryStatus.Late:
                delivery.MarkAsLate();
                break;
        }

        return delivery;
    }

    private static ContractPenalty CreatePenalty(
        Guid planId,
        ContractPenaltyStatus status)
    {
        var penalty = new ContractPenalty(
            planId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(100).Value!,
            Price.Create(100).Value!,
            Price.Create(100).Value!);

        switch (status)
        {
            case ContractPenaltyStatus.Paid:
                penalty.Pay(Guid.NewGuid());
                break;
            case ContractPenaltyStatus.Waived:
                penalty.Waive(Guid.NewGuid(), "reason with enough length");
                break;
            case ContractPenaltyStatus.Canceled:
                penalty.Cancel(Guid.NewGuid(), "reason with enough length");
                break;
            case ContractPenaltyStatus.Overdue:
                typeof(ContractPenalty)
                    .GetProperty(nameof(ContractPenalty.DueDate))!
                    .SetValue(penalty, DateTime.UtcNow.Date.AddDays(-1));
                penalty.MarkAsOverdue();
                break;
        }

        return penalty;
    }

    private static DowngradePlanInput CreateValidInput(Guid productId, int quantity = 1)
    {
        return new DowngradePlanInput
        {
            Reason = "Customer requested lower plan",
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            ]
        };
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PlanEntity?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), new DowngradePlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Canceled()
    {
        var plan = CreatePlan(PlanStatus.Canceled);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new DowngradePlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Open_Penalty()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreatePenalty(plan.Id, ContractPenaltyStatus.PendingPayment)
            ]);

        var result = await _useCase.Execute(plan.Id, new DowngradePlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Is_Overdue_For_More_Than_15_Days()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId))
            .ReturnsAsync([
                CreateBilling(plan.Id, 1, BillingStatus.Late, DateTime.UtcNow.AddDays(-20))
            ]);

        var result = await _useCase.Execute(plan.Id, new DowngradePlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Pending_Delivery_Is_Scheduled_For_Today()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateDelivery(plan.Id, 1, DeliveryStatus.Pending, DateTime.UtcNow)
            ]);

        var result = await _useCase.Execute(plan.Id, new DowngradePlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_All_Items_Are_Removed()
    {
        var plan = CreatePlan();
        var product = CreateProduct();

        plan.AddItem(new PlanItem(plan.Id, product.Id, StockQuantity.Create(2).Value!));

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
            CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(5))
        ]);

        var result = await _useCase.Execute(plan.Id, CreateValidInput(product.Id, 0));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Product_Not_Found()
    {
        var plan = CreatePlan();
        var productId = Guid.NewGuid();

        plan.AddItem(new PlanItem(plan.Id, productId, StockQuantity.Create(2).Value!));

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
            CreateBilling(plan.Id, 1, BillingStatus.Pending, DateTime.UtcNow.AddDays(5))
        ]);
        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(plan.Id, CreateValidInput(productId, 1));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Downgrade_Plan_Successfully()
    {
        var plan = CreatePlan();
        var product = CreateProduct(price: 100m);

        plan.AddItem(new PlanItem(plan.Id, product.Id, StockQuantity.Create(3).Value!));

        var deliveries = new List<Delivery>
        {
            CreateDelivery(plan.Id, 1, DeliveryStatus.Delivered, DateTime.UtcNow.AddDays(-30)),
            CreateDelivery(plan.Id, 2, DeliveryStatus.Pending, DateTime.UtcNow.AddDays(5)),
            CreateDelivery(plan.Id, 3, DeliveryStatus.Pending, DateTime.UtcNow.AddDays(35))
        };

        var billings = new List<Billing>
        {
            CreateBilling(plan.Id, 1, BillingStatus.Paid, DateTime.UtcNow.AddDays(-30), 100m),
            CreateBilling(plan.Id, 2, BillingStatus.Pending, DateTime.UtcNow.AddDays(5), 100m),
            CreateBilling(plan.Id, 3, BillingStatus.Pending, DateTime.UtcNow.AddDays(35), 100m)
        };

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync(deliveries);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync(billings);
        _productRepo.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);

        _calc.Setup(x => x.CalculateContractTotal(
            It.IsAny<decimal>(),
            It.IsAny<int>(),
            It.IsAny<Discount?>()))
            .Returns(Result<Price>.Success(Price.Create(200).Value!));

        _calc.Setup(x => x.CalculateMonthlyBilling(
            It.IsAny<Price>(),
            It.IsAny<int>()))
            .Returns(Result<Price>.Success(Price.Create(100).Value!));

        _dateService.Setup(x => x.AdjustDay(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>()))
            .Returns((int year, int month, int day) =>
                new DateTime(year, month, Math.Min(day, 28)));

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("System"));

        var result = await _useCase.Execute(plan.Id, CreateValidInput(product.Id, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlanId.Should().Be(plan.Id);
        result.Value.NewTotal.Should().BeLessThan(result.Value.PreviousTotal);
        result.Value.Penalty.Should().NotBeNull();

        _penaltyRepo.Verify(x => x.AddAsync(It.IsAny<ContractPenalty>()), Times.Once);
        _planRepo.Verify(x => x.Update(plan), Times.Once);
        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "Plan",
            plan.Id,
            It.IsAny<object>(),
            It.IsAny<object>()), Times.Once);
    }
}

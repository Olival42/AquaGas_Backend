using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using FluentAssertions;
using Moq;
using Xunit;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class UpgradePlanTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IPlanDateService> _dateService = new();
    private readonly Mock<IPlanCalculationService> _calc = new();
    private readonly Mock<IPlanDeliveryService> _deliveryService = new();
    private readonly Mock<IPlanBillingService> _billingService = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private readonly UpgradePlan _useCase;

    public UpgradePlanTests()
    {
        _useCase = new UpgradePlan(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _productRepo.Object,
            _userContext.Object,
            _dateService.Object,
            _calc.Object,
            _deliveryService.Object,
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
        DateTime.UtcNow.AddDays(-10),
        endDate ?? DateTime.UtcNow.AddDays(10),
        10,
        10,
        null);

    switch (status)
    {
        case PlanStatus.Finished:
            plan.Finish();
            break;

        case PlanStatus.Suspended:
            plan.Suspend("test");
            break;

        case PlanStatus.Canceled:
            plan.Cancel();
            break;

        case PlanStatus.AwaitingClosure:
            plan.SetAwaitingClosure();
            break;
    }

    return plan;
}

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PlanEntity?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), CreateValidUpgradeInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
public async Task Should_Fail_When_Plan_Is_Canceled()
{
    var plan = CreatePlan(PlanStatus.Canceled);

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
        .ReturnsAsync(plan);

    var result = await _useCase.Execute(plan.Id, CreateValidUpgradeInput());

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Has_Pending_Penalty()
{
    var plan = CreatePlan();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
        .ReturnsAsync([
            CreatePenalty(ContractPenaltyStatus.PendingPayment)
        ]);

    var result = await _useCase.Execute(plan.Id, CreateValidUpgradeInput());

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Billing_Is_Overdue_15_Days()
{
    var plan = CreatePlan();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);

    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId))
        .ReturnsAsync([
            CreateBilling(BillingStatus.Late, plan.Id, DateTime.UtcNow.AddDays(-20), 1)
        ]);

    var result = await _useCase.Execute(plan.Id, CreateValidUpgradeInput());

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Pending_Delivery_Today()
{
    var plan = CreatePlan();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);

    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
        .ReturnsAsync([
            CreateDelivery(DeliveryStatus.Pending, DateTime.UtcNow)
        ]);

    var result = await _useCase.Execute(plan.Id, CreateValidUpgradeInput());

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Product_Not_Found()
{
    var plan = CreatePlan();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1)
    ]);

    _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync((ProductEntity?)null);

    var input = new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = Guid.NewGuid(),
                Quantity = 2
            }
        ]
    };

    var result = await _useCase.Execute(plan.Id, input);

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Product_Is_Inactive()
{
    var plan = CreatePlan();
    var product = CreateInactiveProduct();

    _productRepo.Setup(x => x.GetByIdAsync(product.Id))
        .ReturnsAsync(product);

    var input = new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = product.Id,
                Quantity = 2
            }
        ]
    };

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1)
    ]);

    var result = await _useCase.Execute(plan.Id, input);

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Upgrade_Plan_Successfully()
{
    var plan = CreatePlan();
    var product = CreateActiveProduct();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1),
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(35), 2)
    ]);

    _productRepo.Setup(x => x.GetByIdAsync(product.Id))
        .ReturnsAsync(product);

    _calc.Setup(x => x.CalculateContractTotal(
        It.IsAny<decimal>(),
        It.IsAny<int>(),
        It.IsAny<Discount?>()))
        .Returns(Result<Price>.Success(Price.Create(2000).Value!));

    _calc.Setup(x => x.CalculateMonthlyBilling(
        It.IsAny<Price>(),
        It.IsAny<int>()))
        .Returns(Result<Price>.Success(Price.Create(200).Value!));

    _userContext.Setup(x => x.GetUserId())
        .Returns(Result<Guid>.Success(Guid.NewGuid()));

    _userContext.Setup(x => x.GetUserName())
        .Returns(Result<string>.Success("System"));

    var input = new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = product.Id,
                Quantity = 2
            }
        ]
    };

    var result = await _useCase.Execute(plan.Id, input);

    result.IsSuccess.Should().BeTrue();
    result.Value!.Message.Should().Be("Plan upgraded successfully");
}

[Fact]
public async Task Should_Fail_When_Custom_Cycle_Without_Duration()
{
    var plan = CreatePlan();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1)
    ]);

    var input = new UpgradePlanInput
    {
        Cycle = PlanCycle.Custom,
        DurationInMonths = null,
        Items = []
    };

    var result = await _useCase.Execute(plan.Id, input);

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_Downgrading_Product_Quantity()
{
    var plan = CreatePlan();

    plan.AddItem(new PlanItem(plan.Id, Guid.NewGuid(), StockQuantity.Create(5).Value!));

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
        .ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1)
    ]);

    var productId = plan.Items.First().ProductId;

    _productRepo.Setup(x => x.GetByIdAsync(productId))
        .ReturnsAsync(CreateActiveProduct());

    var input = new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = productId,
                Quantity = 1 // menor que 5 => downgrade
            }
        ]
    };

    var result = await _useCase.Execute(plan.Id, input);

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_New_Total_Is_Not_Greater()
{
    var plan = CreatePlan();
    var product = CreateActiveProduct();

    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1)
    ]);

    _productRepo.Setup(x => x.GetByIdAsync(product.Id))
        .ReturnsAsync(product);

    _calc.Setup(x => x.CalculateContractTotal(
        It.IsAny<decimal>(),
        It.IsAny<int>(),
        It.IsAny<Discount?>()))
        .Returns(Result<Price>.Success(Price.Create(10).Value!));

    var input = new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = product.Id,
                Quantity = 2
            }
        ]
    };

    var result = await _useCase.Execute(plan.Id, input);

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Fail_When_No_Change_Provided()
{
    var result = await _useCase.Execute(Guid.NewGuid(), new UpgradePlanInput());

    result.IsFailure.Should().BeTrue();
}

[Fact]
public async Task Should_Write_Audit_When_Success()
{
    var plan = CreatePlan();
    var product = CreateActiveProduct();

    SetupPlanSuccess(plan, product);

    _calc.Setup(x => x.CalculateContractTotal(
        It.IsAny<decimal>(),
        It.IsAny<int>(),
        It.IsAny<Discount?>()))
        .Returns(Result<Price>.Success(Price.Create(2000).Value!));

    _calc.Setup(x => x.CalculateMonthlyBilling(
        It.IsAny<Price>(),
        It.IsAny<int>()))
        .Returns(Result<Price>.Success(Price.Create(200).Value!));

    _userContext.Setup(x => x.GetUserId())
        .Returns(Result<Guid>.Success(Guid.NewGuid()));

    _userContext.Setup(x => x.GetUserName())
        .Returns(Result<string>.Success("System"));

    var result = await _useCase.Execute(plan.Id, new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = product.Id,
                Quantity = 2
            }
        ]
    });

    result.IsSuccess.Should().BeTrue();

    _audit.Verify(x => x.LogAsync(
        It.IsAny<Guid>(),
        It.IsAny<string>(),
        AuditAction.UPDATE,
        "Plan",
        plan.Id,
        It.IsAny<object>(),
        It.IsAny<object>()),
        Times.Once);
}

private void SetupPlanSuccess(PlanEntity plan, ProductEntity product)
{
    _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
    _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(plan.CustomerId)).ReturnsAsync([]);
    _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
    _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(5), 1),
        CreateBilling(BillingStatus.Pending, plan.Id, DateTime.UtcNow.AddDays(35), 2)
    ]);
    _productRepo.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);
}

private static UpgradePlanInput CreateValidUpgradeInput()
{
    return new UpgradePlanInput
    {
        Items =
        [
            new UpgradePlanItemInput
            {
                ProductId = Guid.NewGuid(),
                Quantity = 2
            }
        ]
    };
}

private static ContractPenalty CreatePenalty(ContractPenaltyStatus status)
{
    var penalty = new ContractPenalty(
        Guid.NewGuid(),
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

private static Billing CreateBilling(
    BillingStatus status,
    Guid planId,
    DateTime dueDate,
    int period)
{
    var billing = new Billing(
        planId,
        period,
        dueDate,
        Price.Create(100).Value!);

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
                Price.Create(100).Value!);
            billing.MarkAsLate();
            break;
    }

    return billing;
}

private static Delivery CreateDelivery(
    DeliveryStatus status,
    DateTime dueDate)
{
    var delivery = new Delivery(
        Guid.NewGuid(),
        1,
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

private static ProductEntity CreateActiveProduct()
{
    return new ProductEntity(
        ProductName.Create("Produto teste").Value!,
        TypeProduct.Water,
        Price.Create(150).Value!,
        StockQuantity.Create(10).Value!);
}

private static ProductEntity CreateInactiveProduct()
{
    var product = new ProductEntity(
        ProductName.Create("Produto inativo").Value!,
        TypeProduct.Water,
        Price.Create(150).Value!,
        StockQuantity.Create(0).Value!);

    product.Deactivate();

    return product;
}
}

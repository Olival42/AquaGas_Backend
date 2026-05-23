using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using ProductEntity = AquaGas.Product.Domain.Models.Product;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class ConfirmDeliveryTests
{
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly ConfirmDelivery _useCase;

    public ConfirmDeliveryTests()
    {
        _useCase = new ConfirmDelivery(
            _deliveryRepo.Object,
            _planRepo.Object,
            _billingRepo.Object,
            _productRepo.Object,
            _stockMovementRepo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
    }

    private static PlanEntity CreatePlan()
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(100).Value!,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            10,
            10,
            null);

        plan.AddItem(new PlanItem(plan.Id, Guid.NewGuid(), StockQuantity.Create(2).Value!));
        return plan;
    }

    private static ProductEntity CreateProduct(int quantity = 10)
        => new(
            ProductName.Create("Produto").Value!,
            TypeProduct.Water,
            Price.Create(20).Value!,
            StockQuantity.Create(quantity).Value!);

    [Fact]
    public async Task Should_Fail_When_Delivery_Not_Found()
    {
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Delivery?)null);

        var result = await _useCase.Execute(new ConfirmDeliveryInput { DeliveryId = Guid.NewGuid() });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Overdue_Billing()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);
        var overdueBilling = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(-2), Price.Create(100).Value!);
        overdueBilling.MarkAsLate();

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id)).ReturnsAsync(delivery);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([overdueBilling]);

        var result = await _useCase.Execute(new ConfirmDeliveryInput { DeliveryId = delivery.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_Before_Scheduled_Date()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.AddDays(1));

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id)).ReturnsAsync(delivery);

        var result = await _useCase.Execute(new ConfirmDeliveryInput { DeliveryId = delivery.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Stock_Is_Insufficient()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);
        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(1), Price.Create(100).Value!);
        var product = CreateProduct(quantity: 1);
        var planItem = plan.Items.First();

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id)).ReturnsAsync(delivery);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([billing]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([delivery]);
        _productRepo.Setup(x => x.GetByIdAsync(planItem.ProductId)).ReturnsAsync(product);

        var result = await _useCase.Execute(new ConfirmDeliveryInput { DeliveryId = delivery.Id });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Confirm_Delivery_Successfully()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);
        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(1), Price.Create(100).Value!);
        var product = CreateProduct();
        var planItem = plan.Items.First();

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id)).ReturnsAsync(delivery);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([billing]);
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([delivery]);
        _productRepo.Setup(x => x.GetByIdAsync(planItem.ProductId)).ReturnsAsync(product);

        var result = await _useCase.Execute(new ConfirmDeliveryInput { DeliveryId = delivery.Id });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DeliveryStatus.Delivered.ToString());
        _productRepo.Verify(x => x.Update(It.IsAny<ProductEntity>()), Times.Once);
        _stockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.Once);
        _deliveryRepo.Verify(x => x.Update(delivery), Times.AtLeastOnce);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(plan.Id), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_Canceled()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);
        delivery.Cancel();

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("canceled");
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Canceled()
    {
        var plan = CreatePlan();
        typeof(PlanEntity)
            .GetProperty(nameof(PlanEntity.Status))!
            .SetValue(plan, PlanStatus.Canceled);

        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Suspended()
    {
        var plan = CreatePlan();
        typeof(PlanEntity)
            .GetProperty(nameof(PlanEntity.Status))!
            .SetValue(plan, PlanStatus.Suspended);

        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Is_Late()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(-2), Price.Create(100).Value!);
        billing.MarkAsLate();

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing> { billing });

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Is_Pending_But_Overdue()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(-5), Price.Create(100).Value!);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing> { billing });

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Previous_Delivery_Is_Pending()
    {
        var plan = CreatePlan();

        var previous = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);
        var current = new Delivery(plan.Id, 2, DateTime.UtcNow.Date);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(current.Id))
            .ReturnsAsync(current);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Delivery>
            {
            previous,
            current
            });

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing>());

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = current.Id
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Create_StockMovement_For_Each_PlanItem()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing>
            {
            new(plan.Id, 1, DateTime.UtcNow.AddDays(1), Price.Create(100).Value!)
            });

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Delivery> { delivery });

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(new ConfirmDeliveryInput
        {
            DeliveryId = delivery.Id
        });

        result.IsSuccess.Should().BeTrue();

        _stockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.AtLeastOnce);
    }
}

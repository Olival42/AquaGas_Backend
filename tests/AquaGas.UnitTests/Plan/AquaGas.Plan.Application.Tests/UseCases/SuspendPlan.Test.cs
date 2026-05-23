using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using FluentAssertions;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using Moq;
using Xunit;
using AquaGas.Plan.Domain.Models;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class SuspendPlanTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private readonly SuspendPlan _useCase;

    public SuspendPlanTests()
    {
        _useCase = new SuspendPlan(
            _planRepo.Object,
            _userContext.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _audit.Object);
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PlanEntity?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), new SuspendPlanInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Not_Active()
    {
        var plan = CreatePlan(PlanStatus.Finished);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_User_Context_Is_Invalid()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Validation("invalid")));

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_Scheduled_Today()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("system"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateDelivery(
                    DeliveryStatus.Pending,
                    DateTime.UtcNow.Date.AddHours(23))
            ]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Suspend_Plan_Successfully()
    {
        var plan = CreatePlan(PlanStatus.Active);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("system"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateDelivery(DeliveryStatus.Pending, DateTime.UtcNow.AddDays(1))
            ]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateBilling(BillingStatus.Pending)
            ]);

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput
        {
            Reason = "test"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Suspended");

        _planRepo.Verify(x => x.Update(plan), Times.Once);
    }

    [Fact]
    public async Task Should_Cancel_Billings_And_Deliveries()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("system"));

        var delivery = CreateDelivery(DeliveryStatus.Pending, DateTime.UtcNow.AddDays(2));
        var billing = CreateBilling(BillingStatus.Pending);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([delivery]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([billing]);

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput
        {
            Reason = "test"
        });

        result.IsSuccess.Should().BeTrue();

        _deliveryRepo.Verify(x => x.Update(It.IsAny<Delivery>()), Times.Once);
        _billingRepo.Verify(x => x.Update(It.IsAny<Billing>()), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Active()
    {
        var plan = CreatePlan(PlanStatus.Suspended);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput { Reason = "x" });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Scheduled_Today()
    {
        var plan = CreatePlan(PlanStatus.Active);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Delivery>
            {
            new(plan.Id, 1, DateTime.UtcNow.Date.AddHours(23))
            });

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing>());

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput { Reason = "x" });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Invalid()
    {
        var plan = CreatePlan(PlanStatus.Active);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("no user")));

        var result = await _useCase.Execute(plan.Id, new SuspendPlanInput { Reason = "x" });

        result.IsFailure.Should().BeTrue();
    }

    private static PlanEntity CreatePlan(PlanStatus status = PlanStatus.Active)
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(1000).Value!,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(10),
            10,
            10,
            null);

        if (status == PlanStatus.Finished)
            plan.Finish();

        if (status == PlanStatus.Suspended)
            plan.Suspend("test");

        return plan;
    }

    private static Billing CreateBilling(BillingStatus status)
    {
        var billing = new Billing(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            Price.Create(100).Value!);

        if (status == BillingStatus.Paid)
            billing.Pay(Guid.NewGuid());

        if (status == BillingStatus.Late)
            billing.MarkAsLate();

        return billing;
    }

    private static Delivery CreateDelivery(DeliveryStatus status, DateTime dueDate)
    {
        var delivery = new Delivery(
            Guid.NewGuid(),
            1,
            dueDate);

        if (status == DeliveryStatus.Delivered)
            delivery.Complete();

        return delivery;
    }
}

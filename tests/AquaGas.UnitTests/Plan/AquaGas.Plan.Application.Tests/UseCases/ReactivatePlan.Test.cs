using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using FluentAssertions;
using Moq;
using Xunit;

namespace AquaGas.Plan.Application.Tests.UseCases;

public class ReactivatePlanTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<IPlanDateService> _dateService = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private readonly Guid _planId = Guid.NewGuid();

    public ReactivatePlanTests()
    {
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContractPenalty>());
    }

    private ReactivatePlan CreateSut()
        => new(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _dateService.Object,
            _userContext.Object,
            _audit.Object);

    private PlanEntity CreatePlan(PlanStatus status = PlanStatus.Suspended)
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(100).Value!,
            DateTime.UtcNow.AddMonths(-2),
            DateTime.UtcNow.AddMonths(2),
            10,
            10,
            null);

        if (status == PlanStatus.Suspended)
            plan.Suspend("test");

        return plan;
    }

    private void SetupUser()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));
    }

    private static Billing CreateBilling(BillingStatus status)
    {
        var billing = new Billing(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow.AddDays(1),
            Price.Create(100).Value!);

        switch (status)
        {
            case BillingStatus.Paid:
                billing.Pay(Guid.NewGuid());
                break;

            case BillingStatus.Late:
                billing.MarkAsLate();
                break;
        }

        return billing;
    }

    private static Billing CreatePastDuePendingBilling()
    {
        return new Billing(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow.AddDays(-2),
            Price.Create(100).Value!);
    }

    private static Delivery CreateDelivery(DeliveryStatus status)
    {
        var delivery = new Delivery(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow.AddDays(1));

        switch (status)
        {
            case DeliveryStatus.Delivered:
                delivery.Complete();
                break;

            case DeliveryStatus.Cancelled:
                delivery.Cancel();
                break;

            case DeliveryStatus.Late:
                delivery.MarkAsLate();
                break;
        }

        return delivery;
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

        if (status == ContractPenaltyStatus.PendingPayment)
            return penalty;

        if (status == ContractPenaltyStatus.Paid)
            penalty.Pay(Guid.NewGuid());

        if (status == ContractPenaltyStatus.Canceled)
            penalty.Cancel(Guid.NewGuid(), "reason with enough length");

        if (status == ContractPenaltyStatus.Waived)
            penalty.Waive(Guid.NewGuid(), "reason with enough length");

        if (status == ContractPenaltyStatus.Overdue)
        {
            typeof(ContractPenalty)
                .GetProperty(nameof(ContractPenalty.DueDate))!
                .SetValue(penalty, DateTime.UtcNow.Date.AddDays(-1));
            penalty.MarkAsOverdue();
        }

        return penalty;
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync((PlanEntity?)null);

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Suspended()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan(PlanStatus.Active));

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_User_Context_Fails()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan());

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("fail")));

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_No_Pending_Schedules()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan());

        SetupUser();

        _billingRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Billing>
            {
            CreateBilling(BillingStatus.Paid)
            });

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Delivery>
            {
            CreateDelivery(DeliveryStatus.Delivered)
            });

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Open_Penalty()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan());

        SetupUser();

        _billingRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Billing>
            {
            CreateBilling(BillingStatus.Pending)
            });

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Delivery>
            {
            CreateDelivery(DeliveryStatus.Pending)
            });

        _penaltyRepo
            .Setup(x => x.GetByPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContractPenalty>
            {
                CreatePenalty(ContractPenaltyStatus.PendingPayment)
            });

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billings_Are_Late_After_Marking()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan());

        SetupUser();

        _billingRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Billing>
            {
            CreatePastDuePendingBilling()
            });

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Delivery>
            {
            CreateDelivery(DeliveryStatus.Pending)
            });

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reactivate_Successfully()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(plan);

        SetupUser();

        var delivery = CreateDelivery(DeliveryStatus.Pending);
        var billing = CreateBilling(BillingStatus.Pending);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Delivery> { delivery });

        _billingRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Billing> { billing });

        _dateService.Setup(x => x.AdjustDay(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns((int y, int m, int d) =>
                new DateTime(y, m, Math.Min(d, 28)));

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsSuccess.Should().BeTrue();

        _planRepo.Verify(x => x.Update(plan), Times.Once);
        _deliveryRepo.Verify(x => x.Update(It.IsAny<Delivery>()), Times.AtLeastOnce);
        _billingRepo.Verify(x => x.Update(It.IsAny<Billing>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Not_Suspended()
    {
        var plan = CreatePlan(PlanStatus.Active);

        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(plan);

        var sut = CreateSut();

        var result = await sut.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Billing_Becomes_Late()
    {
        _planRepo.Setup(x => x.GetByIdAsync(_planId))
            .ReturnsAsync(CreatePlan());

        SetupUser();

        _billingRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Billing>
            {
            CreatePastDuePendingBilling()
            });

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(_planId))
            .ReturnsAsync(new List<Delivery>
            {
            CreateDelivery(DeliveryStatus.Pending)
            });

        var sut = CreateSut();

        var result = await sut.Execute(_planId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reschedule_Deliveries_And_Billings()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(plan);

        var delivery = CreateDelivery(DeliveryStatus.Pending);

        var billing = CreateBilling(BillingStatus.Pending);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Delivery> { delivery });

        _billingRepo.Setup(x => x.GetByPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Billing> { billing });

        _dateService.Setup(x => x.AdjustDay(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>()))
            .Returns(DateTime.UtcNow.AddDays(2));

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        var sut = CreateSut();

        var result = await sut.Execute(plan.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("invalid")));

        var sut = CreateSut();

        var result = await sut.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(Error.Unauthorized("invalid")));

        var sut = CreateSut();

        var result = await sut.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }
}

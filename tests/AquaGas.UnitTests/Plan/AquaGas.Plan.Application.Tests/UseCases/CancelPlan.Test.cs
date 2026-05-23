using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class CancelPlanTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<IContractPenaltyService> _penaltyService = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private readonly CancelPlan _useCase;

    public CancelPlanTests()
    {
        _useCase = new CancelPlan(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _penaltyService.Object,
            _userContext.Object,
            _audit.Object);
    }

    private static PlanEntity CreatePlan(PlanStatus status = PlanStatus.Active)
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(1000).Value!,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            10,
            10,
            null);

        if (status == PlanStatus.Suspended)
            plan.Suspend("test");
        if (status == PlanStatus.Canceled)
            plan.Cancel();
        if (status == PlanStatus.Finished)
            plan.Finish();

        return plan;
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        _planRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PlanEntity?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), new CancelPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Open_Penalty()
    {
        var plan = CreatePlan();
        var penalty = new ContractPenalty(plan.Id, Guid.NewGuid(), Guid.NewGuid(), ContractPenaltyType.EarlyCancellation,
            Price.Create(100).Value!, Price.Create(100).Value!, Price.Create(100).Value!);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([penalty]);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Is_Finished()
    {
        var plan = CreatePlan(PlanStatus.Finished);
        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Delivery_Scheduled_For_Today()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date.AddHours(23));
        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(2), Price.Create(100).Value!);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([delivery]);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([billing]);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "cancelamento" });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Cancel_Plan_Successfully()
    {
        var plan = CreatePlan();
        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.AddDays(2));
        var billing = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(2), Price.Create(100).Value!);
        var generatedPenalty = new ContractPenalty(plan.Id, Guid.NewGuid(), Guid.NewGuid(), ContractPenaltyType.EarlyCancellation,
            Price.Create(1000).Value!, Price.Create(200).Value!, Price.Create(200).Value!);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));
        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([delivery]);
        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([billing]);
        _penaltyService.Setup(x => x.CalculateCancellationPenalty(plan, It.IsAny<List<Billing>>(), It.IsAny<Guid>(), It.IsAny<string?>()))
            .Returns(generatedPenalty);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "cancelamento" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PlanStatus.Canceled.ToString());
        _planRepo.Verify(x => x.Update(plan), Times.Once);
        _penaltyRepo.Verify(x => x.AddAsync(It.IsAny<ContractPenalty>()), Times.Once);
        _planRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Already_Canceled()
    {
        var plan = CreatePlan(PlanStatus.Canceled);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Status_Is_Invalid()
    {
        var plan = CreatePlan();
        plan.Finish();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Should_Fail_When_User_Context_Is_Invalid()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Has_Delivery_For_Today()
    {
        var plan = CreatePlan();

        var delivery = new Delivery(plan.Id, 1, DateTime.UtcNow.Date);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([delivery]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(2), Price.Create(100).Value!)
            ]);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Should_Cancel_Only_Pending_Items()
    {
        var plan = CreatePlan();

        var pendingDelivery = new Delivery(plan.Id, 1, DateTime.UtcNow.AddDays(2));
        var lateDelivery = new Delivery(plan.Id, 2, DateTime.UtcNow.AddDays(-2));
        lateDelivery.MarkAsLate();

        var paidBilling = new Billing(plan.Id, 1, DateTime.UtcNow.AddDays(2), Price.Create(100).Value!);
        paidBilling.Pay(Guid.NewGuid());

        var pendingBilling = new Billing(plan.Id, 2, DateTime.UtcNow.AddDays(2), Price.Create(100).Value!);

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id)).ReturnsAsync([]);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([pendingDelivery, lateDelivery]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([paidBilling, pendingBilling]);

        _penaltyService.Setup(x => x.CalculateCancellationPenalty(
            It.IsAny<PlanEntity>(),
            It.IsAny<List<Billing>>(),
            It.IsAny<Guid>(),
            It.IsAny<string?>()))
            .Returns((ContractPenalty?)null);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsSuccess.Should().BeTrue();

        _deliveryRepo.Verify(x => x.Update(It.Is<Delivery>(d => d.Id == pendingDelivery.Id)), Times.Once);
        _deliveryRepo.Verify(x => x.Update(It.Is<Delivery>(d => d.Id == lateDelivery.Id)), Times.Once);
        _billingRepo.Verify(x => x.Update(It.Is<Billing>(b => b.Id == pendingBilling.Id)), Times.Once);
        _billingRepo.Verify(x => x.Update(It.Is<Billing>(b => b.Id == paidBilling.Id)), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Create_Penalty_When_Zero_Value()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _penaltyService.Setup(x => x.CalculateCancellationPenalty(
            It.IsAny<PlanEntity>(),
            It.IsAny<List<Billing>>(),
            It.IsAny<Guid>(),
            It.IsAny<string?>()))
            .Returns((ContractPenalty?)null);

        var result = await _useCase.Execute(plan.Id, new CancelPlanInput { Reason = "test" });

        result.IsSuccess.Should().BeTrue();

        _penaltyRepo.Verify(x => x.AddAsync(It.IsAny<ContractPenalty>()), Times.Never);
    }
}

using AquaGas.Application.Services;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;

using FluentAssertions;

using Moq;

using Xunit;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanLifecycleServiceTests
{
    private readonly Mock<IPlanRepository> _planRepository = new();
    private readonly Mock<IBillingRepository> _billingRepository = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepository = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private readonly PlanLifecycleService _service;

    public PlanLifecycleServiceTests()
    {
        _service = new PlanLifecycleService(
            _planRepository.Object,
            _billingRepository.Object,
            _deliveryRepository.Object,
            _penaltyRepository.Object,
            _auditLogService.Object);
    }

    [Fact]
    public async Task Should_Return_When_Plan_Does_Not_Exist()
    {
        var planId = Guid.NewGuid();

        _planRepository
            .Setup(x => x.GetByIdAsync(planId))
            .ReturnsAsync((PlanEntity?)null);

        await _service.TryCompletePlanAsync(planId);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);

        _auditLogService.Verify(
            x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_When_Plan_Is_Canceled()
    {
        var plan = CreatePlan(PlanStatus.Canceled);

        SetupPlan(plan);

        await _service.TryCompletePlanAsync(plan.Id);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_When_Plan_Is_Suspended()
    {
        var plan = CreatePlan(PlanStatus.Suspended);

        SetupPlan(plan);

        await _service.TryCompletePlanAsync(plan.Id);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_When_Plan_Is_Already_Finished()
    {
        var plan = CreatePlan(PlanStatus.Finished);

        SetupPlan(plan);

        await _service.TryCompletePlanAsync(plan.Id);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_When_EndDate_Has_Not_Been_Reached()
    {
        var plan = CreatePlan(
            PlanStatus.Active,
            DateTime.UtcNow.AddDays(5));

        SetupPlan(plan);

        await _service.TryCompletePlanAsync(plan.Id);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Finish_Plan_When_All_Items_Are_Closed()
    {
        var plan = CreatePlan(
            PlanStatus.Active,
            DateTime.UtcNow.AddDays(-1));

        SetupPlan(plan);

        _billingRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateBilling(BillingStatus.Paid)
            ]);

        _deliveryRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateDelivery(DeliveryStatus.Delivered)
            ]);

        _penaltyRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        await _service.TryCompletePlanAsync(plan.Id);

        plan.Status.Should().Be(PlanStatus.Finished);

        _planRepository.Verify(
            x => x.Update(plan),
            Times.Once);

        _planRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);

        _auditLogService.Verify(
            x => x.LogAsync(
                null,
                "System",
                AuditAction.UPDATE,
                "Plan",
                plan.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Set_AwaitingClosure_When_Has_Open_Billing()
    {
        var plan = CreatePlan(
            PlanStatus.Active,
            DateTime.UtcNow.AddDays(-1));

        SetupPlan(plan);

        _billingRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateBilling(BillingStatus.Pending)
            ]);

        _deliveryRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateDelivery(DeliveryStatus.Delivered)
            ]);

        _penaltyRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        await _service.TryCompletePlanAsync(plan.Id);

        plan.Status.Should().Be(PlanStatus.AwaitingClosure);

        _planRepository.Verify(
            x => x.Update(plan),
            Times.Once);
    }

    [Fact]
    public async Task Should_Set_AwaitingClosure_When_Has_Open_Delivery()
    {
        var plan = CreatePlan(
            PlanStatus.Active,
            DateTime.UtcNow.AddDays(-1));

        SetupPlan(plan);

        _billingRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateBilling(BillingStatus.Paid)
            ]);

        _deliveryRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateDelivery(DeliveryStatus.Pending)
            ]);

        _penaltyRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        await _service.TryCompletePlanAsync(plan.Id);

        plan.Status.Should().Be(PlanStatus.AwaitingClosure);
    }

    [Fact]
    public async Task Should_Set_AwaitingClosure_When_Has_Open_Penalty()
    {
        var plan = CreatePlan(
            PlanStatus.Active,
            DateTime.UtcNow.AddDays(-1));

        SetupPlan(plan);

        _billingRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateBilling(BillingStatus.Paid)
            ]);

        _deliveryRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateDelivery(DeliveryStatus.Delivered)
            ]);

        _penaltyRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreatePenalty(ContractPenaltyStatus.PendingPayment)
            ]);

        await _service.TryCompletePlanAsync(plan.Id);

        plan.Status.Should().Be(PlanStatus.AwaitingClosure);
    }

    [Fact]
    public async Task Should_Not_Update_When_Status_Does_Not_Change()
    {
        var plan = CreatePlan(
            PlanStatus.AwaitingClosure,
            DateTime.UtcNow.AddDays(-1));

        SetupPlan(plan);

        _billingRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(
            [
                CreateBilling(BillingStatus.Pending)
            ]);

        _deliveryRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        _penaltyRepository
            .Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([]);

        await _service.TryCompletePlanAsync(plan.Id);

        _planRepository.Verify(
            x => x.Update(It.IsAny<PlanEntity>()),
            Times.Never);

        _planRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    private void SetupPlan(PlanEntity plan)
    {
        _planRepository
            .Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);
    }

    public static PlanEntity CreatePlan(
        PlanStatus status = PlanStatus.Active,
        DateTime? endDate = null)
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(1000).Value!,
            DateTime.UtcNow.AddDays(-10),
            endDate ?? DateTime.UtcNow.AddDays(1),
            10,
            10,
            null);

        if (status == PlanStatus.Finished)
            plan.Finish();

        if (status == PlanStatus.AwaitingClosure)
            plan.SetAwaitingClosure();

        if (status == PlanStatus.Suspended)
            plan.Suspend("");

        if (status == PlanStatus.Canceled)
            plan.Cancel();

        return plan;
    }

    private static Billing CreateBilling(BillingStatus status)
    {
        var billing = new Billing(
            planId: Guid.NewGuid(),
            period: 1,
            dueDate: DateTime.UtcNow,
            amount: Price.Create(100).Value!);

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
                    planId: Guid.NewGuid(),
                    period: 1,
                    dueDate: DateTime.UtcNow.AddDays(-1),
                    amount: Price.Create(100).Value!);
                billing.MarkAsLate();
                break;
        }

        return billing;
    }

    private static Delivery CreateDelivery(DeliveryStatus status)
    {
        var dueDate = status == DeliveryStatus.Late
            ? DateTime.UtcNow.AddDays(-1)
            : DateTime.UtcNow;

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

    private static ContractPenalty CreatePenalty(
        ContractPenaltyStatus status)
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
                penalty.Waive(Guid.NewGuid(), "Reason with more than ten chars");
                break;
            case ContractPenaltyStatus.Canceled:
                penalty.Cancel(Guid.NewGuid(), "Reason with more than ten chars");
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
}

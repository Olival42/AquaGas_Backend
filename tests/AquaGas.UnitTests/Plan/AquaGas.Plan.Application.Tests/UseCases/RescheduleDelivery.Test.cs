using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using Moq;
using Xunit;
using FluentAssertions;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class RescheduleDeliveryTests
{
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();

    private RescheduleDelivery CreateSut()
        => new(
            _deliveryRepo.Object,
            _planRepo.Object,
            _userContext.Object,
            _audit.Object);

    [Fact]
    public async Task Should_Fail_When_Delivery_Not_Found()
    {
        _deliveryRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Delivery?)null);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            NewDate = DateTime.UtcNow.AddDays(1),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Plan_Not_Found()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));

        _deliveryRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(delivery.PlanId))
            .ReturnsAsync((PlanEntity?)null);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = delivery.DueDate.AddDays(1),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(PlanStatus.Canceled)]
    [InlineData(PlanStatus.Suspended)]
    public async Task Should_Fail_When_Plan_Is_Not_Allowed(PlanStatus status)
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));
        var plan = new PlanEntity(Guid.NewGuid(), Guid.NewGuid(), PlanCycle.Monthly,
            Price.Create(100).Value!, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 1, 1, null);

        if (status == PlanStatus.Suspended)
            plan.Suspend("test");
        if (status == PlanStatus.Canceled)
            plan.Cancel();

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(delivery.PlanId))
            .ReturnsAsync(plan);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = delivery.DueDate.AddDays(1),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_Not_Canceled()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));
        delivery.MarkAsLate(); // qualquer status diferente de Canceled

        var plan = CreateValidPlan(delivery.PlanId);

        Setup(delivery, plan);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = delivery.DueDate.AddDays(1),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_NewDate_Is_Earlier()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(5));

        var plan = CreateValidPlan(delivery.PlanId);

        SetupCanceledDelivery(delivery, plan);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = delivery.DueDate.AddDays(-1),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Diff_Is_Greater_Than_7_Days()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));

        var plan = CreateValidPlan(delivery.PlanId);

        SetupCanceledDelivery(delivery, plan);

        var sut = CreateSut();

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = delivery.DueDate.AddDays(10),
            Reason = "x"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reschedule_Delivery_Successfully()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));
        delivery.Cancel(); // precisa estar cancelado

        var plan = CreateValidPlan(delivery.PlanId);

        SetupCanceledDelivery(delivery, plan);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var sut = CreateSut();

        var newDate = delivery.DueDate.AddDays(3);

        var result = await sut.Execute(new RescheduleDeliveryInput
        {
            DeliveryId = delivery.Id,
            NewDate = newDate,
            Reason = "fix schedule"
        });

        result.IsSuccess.Should().BeTrue();

        _deliveryRepo.Verify(x => x.Update(delivery), Times.Once);
        _deliveryRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private void Setup(Delivery delivery, PlanEntity plan)
    {
        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _planRepo.Setup(x => x.GetByIdAsync(delivery.PlanId))
            .ReturnsAsync(plan);
    }

    private void SetupCanceledDelivery(Delivery delivery, PlanEntity plan)
    {
        delivery.Cancel();

        Setup(delivery, plan);
    }

    private PlanEntity CreateValidPlan(Guid planId)
    {
        return new PlanEntity(
            planId,
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(100).Value!,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            1,
            1,
            null);
    }
}

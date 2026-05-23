using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class CancelDeliveryTests
{
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly CancelDelivery _useCase;

    public CancelDeliveryTests()
    {
        _useCase = new CancelDelivery(
            _deliveryRepo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Not_Found()
    {
        _deliveryRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Delivery?)null);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            Reason = "reason"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_Today()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.Date.AddHours(23));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "reason"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Already_Delivered()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));
        delivery.Complete();

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "reason"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Is_In_The_Past()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(-1));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "reason"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Cancel_Delivery_Successfully()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(1));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "customer request"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(DeliveryStatus.Canceled.ToString());

        _deliveryRepo.Verify(x => x.Update(delivery), Times.Once);
        _deliveryRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(delivery.PlanId), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Delivery_Already_Canceled()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(2));
        delivery.Cancel();

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "test"
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Context_Is_Invalid()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(2));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "test"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Context_Is_Invalid()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(2));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail());

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "test"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Call_AuditLog_When_Cancel_Success()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(2));

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "customer request"
        });

        result.IsSuccess.Should().BeTrue();

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "Delivery",
            delivery.Id,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Lifecycle_When_Failure()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.Date);

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "test"
        });

        result.IsFailure.Should().BeTrue();

        _lifecycle.Verify(
            x => x.TryCompletePlanAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Update_Delivery_When_Failure()
    {
        var delivery = new Delivery(Guid.NewGuid(), 1, DateTime.UtcNow.Date);

        _deliveryRepo.Setup(x => x.GetByIdAsync(delivery.Id))
            .ReturnsAsync(delivery);

        var result = await _useCase.Execute(new CancelDeliveryInput
        {
            DeliveryId = delivery.Id,
            Reason = "test"
        });

        result.IsFailure.Should().BeTrue();

        _deliveryRepo.Verify(x => x.Update(It.IsAny<Delivery>()), Times.Never);
    }
}

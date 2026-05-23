using System;
using System.Threading.Tasks;
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
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using FluentAssertions;
using Moq;
using Xunit;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class WaiveContractPenaltyTests
{
    private readonly Mock<IContractPenaltyRepository> _repo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly WaiveContractPenalty _useCase;

    public WaiveContractPenaltyTests()
    {
        _useCase = new WaiveContractPenalty(
            _repo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
    }

    private static ContractPenalty CreatePenalty(ContractPenaltyStatus status)
    {
        var penalty = new ContractPenalty(
            planId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            initiatedBy: Guid.NewGuid(),
            type: ContractPenaltyType.EarlyCancellation,
            originalValue: Price.Create(100).Value!,
            remainingValue: Price.Create(100).Value!,
            calculatedAmount: Price.Create(100).Value!,
            notes: null);

        if (status == ContractPenaltyStatus.Waived)
        {
            penalty.Waive(Guid.NewGuid(), "already waived");
        }

        if (status == ContractPenaltyStatus.Canceled)
        {
            penalty.Cancel(
                penalty.InitiatedBy,
                "canceled with valid reason");
        }

        if (status == ContractPenaltyStatus.Paid)
        {
            penalty.Pay(penalty.InitiatedBy);
        }

        return penalty;
    }

    private static WaiveContractPenaltyInput Input()
        => new() { Reason = "Customer request" };

    [Fact]
    public async Task Should_Return_NotFound_When_Penalty_Does_Not_Exist()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ContractPenalty?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), Input());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Contract penalty not found");

        _repo.Verify(x => x.Update(It.IsAny<ContractPenalty>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Already_Paid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Paid);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Penalty is already paid and cannot be waived");
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Already_Waived()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Waived);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Penalty is already waived");
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Canceled()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Canceled);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Cancelled penalties cannot be waived");
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Invalid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Validation("invalid user")));

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Successfully_Waive_Penalty()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("John"));

        _repo.Setup(x => x.Update(It.IsAny<ContractPenalty>()));

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsSuccess.Should().BeTrue();
        result.Value!.PenaltyId.Should().Be(penalty.Id);
        result.Value.Status.Should().Be(ContractPenaltyStatus.Waived.ToString());
        result.Value.Reason.Should().Be("Customer request");

        _repo.Verify(x => x.Update(penalty), Times.Once);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "ContractPenalty",
            penalty.Id,
            It.IsAny<object>(),
            It.IsAny<object>()),
            Times.Once);

        _lifecycle.Verify(x => x.TryCompletePlanAsync(penalty.PlanId), Times.Once);
    }

    [Fact]
    public async Task Should_Fallback_UserName_When_Null()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success(null!));

        var result = await _useCase.Execute(penalty.Id, Input());

        result.IsSuccess.Should().BeTrue();

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid?>(),
            "System",
            AuditAction.UPDATE,
            "ContractPenalty",
            penalty.Id,
            It.IsAny<object>(),
            It.IsAny<object>()),
            Times.Once);
    }
}

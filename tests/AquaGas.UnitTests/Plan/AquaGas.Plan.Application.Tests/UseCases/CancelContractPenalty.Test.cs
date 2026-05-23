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

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class CancelContractPenaltyTests
{
    private readonly Mock<IContractPenaltyRepository> _repo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly CancelContractPenalty _useCase;

    public CancelContractPenaltyTests()
    {
        _useCase = new CancelContractPenalty(
            _repo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
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
        }

        return penalty;
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Not_Found()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ContractPenalty?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Paid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Paid);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Waived()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Waived);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_User_Context_Is_Invalid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Fail());

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Cancel_Penalty_Successfully()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ContractPenaltyStatus.Canceled.ToString());
        _repo.Verify(x => x.Update(penalty), Times.Once);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(penalty.PlanId), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Already_Canceled()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Canceled);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Should_Fail_When_Status_Does_Not_Allow_Cancellation()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Overdue);

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.Status))!
            .SetValue(penalty, (ContractPenaltyStatus)999);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Too_Old()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.Timestamp))!
            .SetValue(penalty, DateTime.UtcNow.AddYears(-2));

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Context_Fails()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail());

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Update_When_Failure_Before_Cancel()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Paid);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsFailure.Should().BeTrue();

        _repo.Verify(x => x.Update(It.IsAny<ContractPenalty>()), Times.Never);
    }

    [Fact]
    public async Task Should_Call_Lifecycle_Only_On_Success()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsSuccess.Should().BeTrue();

        _lifecycle.Verify(x => x.TryCompletePlanAsync(penalty.PlanId), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Only_On_Success()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(penalty.Id, new CancelContractPenaltyInput
        {
            Reason = "reason with enough length"
        });

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}

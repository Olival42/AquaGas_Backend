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
using FluentAssertions;
using Moq;
using Xunit;

namespace AquaGas.Plan.Application.Tests.UseCases;

public sealed class ConfirmContractPenaltyPaymentTests
{
    private readonly Mock<IContractPenaltyRepository> _repo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IPlanLifecycleService> _lifecycle = new();

    private readonly ConfirmContractPenaltyPayment _useCase;

    public ConfirmContractPenaltyPaymentTests()
    {
        _useCase = new ConfirmContractPenaltyPayment(
            _repo.Object,
            _userContext.Object,
            _audit.Object,
            _lifecycle.Object);
    }

    private static ContractPenalty CreatePenalty(
    ContractPenaltyStatus status,
    decimal amount = 100,
    DateTime? timestamp = null)
    {
        var penalty = new ContractPenalty(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(amount).Value!,
            Price.Create(amount).Value!,
            Price.Create(amount).Value!);

        if (status == ContractPenaltyStatus.Paid)
            penalty.Pay(Guid.NewGuid());

        if (status == ContractPenaltyStatus.Waived)
            penalty.Waive(Guid.NewGuid(), "valid reason for waiver");

        if (status == ContractPenaltyStatus.Canceled)
            penalty.Cancel(Guid.NewGuid(), "valid cancel reason");

        if (status == ContractPenaltyStatus.Overdue)
        {
            penalty.MarkAsOverdue();
        }

        return penalty;
    }

    private static Price CreatePriceUnsafe(decimal value)
        => (Price)Activator.CreateInstance(
            typeof(Price),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [value],
            culture: null)!;

    [Fact]
    public async Task Should_Fail_When_Penalty_Not_Found()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ContractPenalty?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Already_Paid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Paid);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Waived()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Waived);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_User_Name_Context_Is_Invalid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Fail());

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Confirm_Payment_Successfully()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);
        _repo.Setup(x => x.GetByIdAsync(penalty.Id)).ReturnsAsync(penalty);
        _userContext.Setup(x => x.GetUserId()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        _userContext.Setup(x => x.GetUserName()).Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(penalty.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ContractPenaltyStatus.Paid.ToString());
        _repo.Verify(x => x.Update(penalty), Times.Once);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(penalty.PlanId), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Status_Is_Not_Allowed()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        // força um status inválido via reflection (simula estado inesperado)
        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.Status))!
            .SetValue(penalty, (ContractPenaltyStatus)999);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Message
            .Should().Contain("does not allow payment");
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

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Message
            .Should().Contain("exceeds the allowed payment period");
    }

    [Fact]
    public async Task Should_Fail_When_Amount_Is_Zero()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.CalculatedAmount))!
            .SetValue(penalty, CreatePriceUnsafe(0));

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("User"));

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("zero amount");
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Invalid()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("invalid user")));

        var result = await _useCase.Execute(penalty.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Penalty_Is_Overdue_But_Not_Payable()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.Overdue);

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.Timestamp))!
            .SetValue(penalty, DateTime.UtcNow);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        var result = await _useCase.Execute(penalty.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ContractPenaltyStatus.Paid.ToString());
    }

    [Fact]
    public async Task Should_Use_Default_UserName_When_Null()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success((string?)null!));

        var result = await _useCase.Execute(penalty.Id);

        result.IsSuccess.Should().BeTrue();

        _audit.Verify(x =>
            x.LogAsync(
                It.IsAny<Guid>(),
                "System",
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Call_All_Persistence_Methods_When_Success()
    {
        var penalty = CreatePenalty(ContractPenaltyStatus.PendingPayment);

        _repo.Setup(x => x.GetByIdAsync(penalty.Id))
            .ReturnsAsync(penalty);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        var result = await _useCase.Execute(penalty.Id);

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(x => x.Update(penalty), Times.Once);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _lifecycle.Verify(x => x.TryCompletePlanAsync(penalty.PlanId), Times.Once);
        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "ContractPenalty",
            penalty.Id,
            It.IsAny<object>(),
            It.IsAny<object>()), Times.Once);
    }
}

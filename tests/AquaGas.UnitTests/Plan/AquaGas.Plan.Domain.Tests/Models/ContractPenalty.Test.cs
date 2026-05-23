using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Domain.Tests.Models;

public class ContractPenaltyTests
{
    private static ContractPenalty CreatePenalty()
    {
        return new ContractPenalty(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(1000).Value!,
            Price.Create(500).Value!,
            Price.Create(500).Value!
        );
    }

    [Fact]
    public void Should_Create_ContractPenalty_Correctly()
    {
        // Arrange & Act
        var penalty = CreatePenalty();

        // Assert
        penalty.Id.Should().NotBeEmpty();

        penalty.Status.Should()
            .Be(ContractPenaltyStatus.PendingPayment);

        penalty.OriginalValue.Value.Should().Be(1000);

        penalty.RemainingValue.Value.Should().Be(500);

        penalty.CalculatedAmount.Value.Should().Be(500);

        penalty.Notes.Should()
            .Be("Multa contratual gerada automaticamente.");

        penalty.PaidDate.Should().BeNull();

        penalty.WaivedAt.Should().BeNull();

        penalty.CanceledAt.Should().BeNull();
    }

    [Fact]
    public void Should_Create_With_Custom_Notes()
    {
        // Arrange & Act
        var penalty = new ContractPenalty(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(1000).Value!,
            Price.Create(500).Value!,
            Price.Create(500).Value!,
            "Multa aplicada manualmente"
        );

        // Assert
        penalty.Notes.Should()
            .Be("Multa aplicada manualmente");
    }

    [Fact]
    public void Should_Pay_Pending_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        var userId = Guid.NewGuid();

        // Act
        penalty.Pay(userId);

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);

        penalty.PaidBy.Should().Be(userId);

        penalty.PaidDate.Should().NotBeNull();
    }

    [Fact]
    public void Should_Pay_Overdue_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.DueDate))!
            .SetValue(penalty, DateTime.UtcNow.AddDays(-5));

        penalty.MarkAsOverdue();

        var userId = Guid.NewGuid();

        // Act
        penalty.Pay(userId);

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);

        penalty.PaidBy.Should().Be(userId);
    }

    [Fact]
    public void Should_Not_Pay_Already_Paid_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        penalty.Pay(Guid.NewGuid());

        var paidDate = penalty.PaidDate;

        // Act
        penalty.Pay(Guid.NewGuid());

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);

        penalty.PaidDate.Should().Be(paidDate);
    }

    [Fact]
    public void Should_Waive_Pending_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        var userId = Guid.NewGuid();

        var reason = "Cliente fidelizado pela empresa";

        // Act
        penalty.Waive(userId, reason);

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Waived);

        penalty.WaivedBy.Should().Be(userId);

        penalty.WaivedAt.Should().NotBeNull();

        penalty.WaiveReason.Should().Be(reason);

        penalty.Notes.Should()
            .Be($"Isenção: {reason}");
    }

    [Fact]
    public void Should_Not_Waive_When_Reason_Is_Invalid()
    {
        // Arrange
        var penalty = CreatePenalty();

        // Act
        penalty.Waive(Guid.NewGuid(), "curto");

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.PendingPayment);

        penalty.WaivedAt.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Waive_Paid_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        penalty.Pay(Guid.NewGuid());

        // Act
        penalty.Waive(
            Guid.NewGuid(),
            "Motivo válido para isenção"
        );

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);
    }

    [Fact]
    public void Should_Cancel_Pending_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        var userId = Guid.NewGuid();

        var reason = "Cobrança gerada incorretamente";

        // Act
        penalty.Cancel(userId, reason);

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Canceled);

        penalty.CanceledBy.Should().Be(userId);

        penalty.CanceledAt.Should().NotBeNull();

        penalty.CancelReason.Should().Be(reason);

        penalty.Notes.Should()
            .Be($"Cancelamento: {reason}");
    }

    [Fact]
    public void Should_Not_Cancel_When_Reason_Is_Too_Short()
    {
        // Arrange
        var penalty = CreatePenalty();

        // Act
        penalty.Cancel(Guid.NewGuid(), "curto");

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.PendingPayment);

        penalty.CanceledAt.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Cancel_When_Reason_Is_Too_Long()
    {
        // Arrange
        var penalty = CreatePenalty();

        var longReason = new string('A', 501);

        // Act
        penalty.Cancel(Guid.NewGuid(), longReason);

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.PendingPayment);

        penalty.CanceledAt.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Cancel_Paid_Penalty()
    {
        // Arrange
        var penalty = CreatePenalty();

        penalty.Pay(Guid.NewGuid());

        // Act
        penalty.Cancel(
            Guid.NewGuid(),
            "Cancelamento válido com motivo"
        );

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);
    }

    [Fact]
    public void Should_Mark_Penalty_As_Overdue()
    {
        // Arrange
        var penalty = CreatePenalty();

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.DueDate))!
            .SetValue(penalty, DateTime.UtcNow.AddDays(-2));

        // Act
        penalty.MarkAsOverdue();

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Overdue);
    }

    [Fact]
    public void Should_Not_Mark_As_Overdue_When_DueDate_Is_Not_Expired()
    {
        // Arrange
        var penalty = CreatePenalty();

        // Act
        penalty.MarkAsOverdue();

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.PendingPayment);
    }

    [Fact]
    public void Should_Not_Mark_Paid_Penalty_As_Overdue()
    {
        // Arrange
        var penalty = CreatePenalty();

        penalty.Pay(Guid.NewGuid());

        typeof(ContractPenalty)
            .GetProperty(nameof(ContractPenalty.DueDate))!
            .SetValue(penalty, DateTime.UtcNow.AddDays(-10));

        // Act
        penalty.MarkAsOverdue();

        // Assert
        penalty.Status.Should()
            .Be(ContractPenaltyStatus.Paid);
    }
}
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Domain.Tests.Models;

public class BillingTests
{
    private static Billing CreateBilling(
        DateTime? dueDate = null)
    {
        return new Billing(
            Guid.NewGuid(),
            1,
            dueDate ?? DateTime.UtcNow.AddDays(5),
            Price.Create(150).Value!
        );
    }

    [Fact]
    public void Should_Create_Billing_Correctly()
    {
        // Arrange & Act
        var dueDate = DateTime.UtcNow.AddDays(10);

        var billing = new Billing(
            Guid.NewGuid(),
            1,
            dueDate,
            Price.Create(200).Value!
        );

        // Assert
        billing.Id.Should().NotBeEmpty();

        billing.PlanId.Should().NotBeEmpty();

        billing.Period.Should().Be(1);

        billing.Status.Should().Be(BillingStatus.Pending);

        billing.Amount.Value.Should().Be(200);

        billing.DueDate.Should().Be(
            DateTime.SpecifyKind(dueDate, DateTimeKind.Utc)
        );

        billing.CreatedAt.Should().NotBe(default);

        billing.PaidAt.Should().BeNull();

        billing.ReceivedBy.Should().BeNull();
    }

    [Fact]
    public void Should_Pay_Pending_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        var userId = Guid.NewGuid();

        // Act
        billing.Pay(userId);

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);

        billing.PaidAt.Should().NotBeNull();

        billing.ReceivedBy.Should().Be(userId);
    }

    [Fact]
    public void Should_Pay_Late_Billing()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(-5)
        );

        billing.MarkAsLate();

        var userId = Guid.NewGuid();

        // Act
        billing.Pay(userId);

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);

        billing.ReceivedBy.Should().Be(userId);
    }

    [Fact]
    public void Should_Not_Pay_Already_Paid_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Pay(Guid.NewGuid());

        var firstPaidAt = billing.PaidAt;

        // Act
        billing.Pay(Guid.NewGuid());

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);

        billing.PaidAt.Should().Be(firstPaidAt);
    }

    [Fact]
    public void Should_Cancel_Pending_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        // Act
        billing.Cancel();

        // Assert
        billing.Status.Should().Be(BillingStatus.Cancelled);
    }

    [Fact]
    public void Should_Cancel_Late_Billing()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(-3)
        );

        billing.MarkAsLate();

        // Act
        billing.Cancel();

        // Assert
        billing.Status.Should().Be(BillingStatus.Cancelled);
    }

    [Fact]
    public void Should_Not_Cancel_Paid_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Pay(Guid.NewGuid());

        // Act
        billing.Cancel();

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);
    }

    [Fact]
    public void Should_Not_Change_Status_When_Cancelling_Already_Cancelled_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Cancel();

        // Act
        billing.Cancel();

        // Assert
        billing.Status.Should().Be(BillingStatus.Cancelled);
    }

    [Fact]
    public void Should_Mark_Billing_As_Late()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(-2)
        );

        // Act
        billing.MarkAsLate();

        // Assert
        billing.Status.Should().Be(BillingStatus.Late);
    }

    [Fact]
    public void Should_Not_Mark_As_Late_When_DueDate_Not_Expired()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(3)
        );

        // Act
        billing.MarkAsLate();

        // Assert
        billing.Status.Should().Be(BillingStatus.Pending);
    }

    [Fact]
    public void Should_Not_Mark_Paid_Billing_As_Late()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Pay(Guid.NewGuid());

        // Act
        billing.MarkAsLate();

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);
    }

    [Fact]
    public void Should_Reschedule_Pending_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        var newDate = DateTime.UtcNow.AddDays(15);

        // Act
        billing.Reschedule(newDate);

        // Assert
        billing.Status.Should().Be(BillingStatus.Pending);

        billing.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Reschedule_Late_Billing()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(-5)
        );

        billing.MarkAsLate();

        var newDate = DateTime.UtcNow.AddDays(10);

        // Act
        billing.Reschedule(newDate);

        // Assert
        billing.Status.Should().Be(BillingStatus.Pending);

        billing.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Reschedule_Cancelled_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Cancel();

        var newDate = DateTime.UtcNow.AddDays(7);

        // Act
        billing.Reschedule(newDate);

        // Assert
        billing.Status.Should().Be(BillingStatus.Pending);

        billing.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Not_Reschedule_Paid_Billing()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Pay(Guid.NewGuid());

        var originalDate = billing.DueDate;

        // Act
        billing.Reschedule(
            DateTime.UtcNow.AddDays(20)
        );

        // Assert
        billing.Status.Should().Be(BillingStatus.Paid);

        billing.DueDate.Should().Be(originalDate);
    }

    [Fact]
    public void Should_Update_Amount_When_Pending()
    {
        // Arrange
        var billing = CreateBilling();

        var newAmount = Price.Create(500).Value!;

        // Act
        billing.UpdateAmount(newAmount);

        // Assert
        billing.Amount.Should().Be(newAmount);

        billing.Amount.Value.Should().Be(500);
    }

    [Fact]
    public void Should_Update_Amount_When_Late()
    {
        // Arrange
        var billing = CreateBilling(
            DateTime.UtcNow.AddDays(-10)
        );

        billing.MarkAsLate();

        var newAmount = Price.Create(800).Value!;

        // Act
        billing.UpdateAmount(newAmount);

        // Assert
        billing.Amount.Should().Be(newAmount);

        billing.Amount.Value.Should().Be(800);
    }

    [Fact]
    public void Should_Not_Update_Amount_When_Paid()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Pay(Guid.NewGuid());

        var originalAmount = billing.Amount;

        // Act
        billing.UpdateAmount(
            Price.Create(999).Value!
        );

        // Assert
        billing.Amount.Should().Be(originalAmount);
    }

    [Fact]
    public void Should_Not_Update_Amount_When_Cancelled()
    {
        // Arrange
        var billing = CreateBilling();

        billing.Cancel();

        var originalAmount = billing.Amount;

        // Act
        billing.UpdateAmount(
            Price.Create(999).Value!
        );

        // Assert
        billing.Amount.Should().Be(originalAmount);
    }
}
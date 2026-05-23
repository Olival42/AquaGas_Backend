using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Domain.Tests.Models;

public class DeliveryTests
{
    private static Delivery CreateDelivery(
        DateTime? dueDate = null)
    {
        return new Delivery(
            Guid.NewGuid(),
            1,
            dueDate ?? DateTime.UtcNow.AddDays(5)
        );
    }

    [Fact]
    public void Should_Create_Delivery_Correctly()
    {
        // Arrange & Act
        var dueDate = DateTime.UtcNow.AddDays(5);

        var delivery = new Delivery(
            Guid.NewGuid(),
            1,
            dueDate
        );

        // Assert
        delivery.Id.Should().NotBeEmpty();

        delivery.Period.Should().Be(1);

        delivery.Status.Should().Be(DeliveryStatus.Pending);

        delivery.DueDate.Should().Be(
            DateTime.SpecifyKind(dueDate, DateTimeKind.Utc)
        );

        delivery.CreatedAt.Should().NotBe(default);

        delivery.DeliveryDate.Should().BeNull();

        delivery.HasCustomSchedule.Should().BeFalse();
    }

    [Fact]
    public void Should_Complete_Pending_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        // Act
        delivery.Complete();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Delivered);

        delivery.DeliveryDate.Should().NotBeNull();
    }

    [Fact]
    public void Should_Complete_Late_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery(
            DateTime.UtcNow.AddDays(-2)
        );

        delivery.MarkAsLate();

        // Act
        delivery.Complete();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Delivered);

        delivery.DeliveryDate.Should().NotBeNull();
    }

    [Fact]
    public void Should_Not_Complete_Cancelled_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Cancel();

        // Act
        delivery.Complete();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Cancelled);

        delivery.DeliveryDate.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Complete_Already_Delivered_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Complete();

        var firstDate = delivery.DeliveryDate;

        // Act
        delivery.Complete();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Delivered);

        delivery.DeliveryDate.Should().Be(firstDate);
    }

    [Fact]
    public void Should_Cancel_Pending_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        // Act
        delivery.Cancel();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Cancelled);
    }

    [Fact]
    public void Should_Cancel_Late_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery(
            DateTime.UtcNow.AddDays(-5)
        );

        delivery.MarkAsLate();

        // Act
        delivery.Cancel();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Cancelled);
    }

    [Fact]
    public void Should_Not_Cancel_Delivered_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Complete();

        // Act
        delivery.Cancel();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public void Should_Mark_Delivery_As_Late()
    {
        // Arrange
        var delivery = CreateDelivery(
            DateTime.UtcNow.AddDays(-1)
        );

        // Act
        delivery.MarkAsLate();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Late);
    }

    [Fact]
    public void Should_Not_Mark_As_Late_When_DueDate_Not_Expired()
    {
        // Arrange
        var delivery = CreateDelivery(
            DateTime.UtcNow.AddDays(2)
        );

        // Act
        delivery.MarkAsLate();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Pending);
    }

    [Fact]
    public void Should_Not_Mark_Delivered_Delivery_As_Late()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Complete();

        // Act
        delivery.MarkAsLate();

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public void Should_Reschedule_Pending_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        var newDate = DateTime.UtcNow.AddDays(10);

        // Act
        delivery.Reschedule(newDate);

        // Assert
        delivery.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );

        delivery.Status.Should().Be(DeliveryStatus.Pending);
    }

    [Fact]
    public void Should_Reschedule_Late_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery(
            DateTime.UtcNow.AddDays(-3)
        );

        delivery.MarkAsLate();

        var newDate = DateTime.UtcNow.AddDays(7);

        // Act
        delivery.Reschedule(newDate);

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Pending);

        delivery.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Reschedule_Cancelled_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Cancel();

        var newDate = DateTime.UtcNow.AddDays(7);

        // Act
        delivery.Reschedule(newDate);

        // Assert
        delivery.Status.Should().Be(DeliveryStatus.Pending);

        delivery.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );
    }

    [Fact]
    public void Should_Not_Reschedule_Delivered_Delivery()
    {
        // Arrange
        var delivery = CreateDelivery();

        delivery.Complete();

        var originalDate = delivery.DueDate;

        // Act
        delivery.Reschedule(
            DateTime.UtcNow.AddDays(20)
        );

        // Assert
        delivery.DueDate.Should().Be(originalDate);

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public void Should_Reschedule_Manually()
    {
        // Arrange
        var delivery = CreateDelivery();

        var newDate = DateTime.UtcNow.AddDays(15);

        // Act
        delivery.RescheduleManually(newDate);

        // Assert
        delivery.DueDate.Should().Be(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc)
        );

        delivery.HasCustomSchedule.Should().BeTrue();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
    }

    [Fact]
    public void Should_Keep_Custom_Schedule_Flag_After_Manual_Reschedule()
    {
        // Arrange
        var delivery = CreateDelivery();

        // Act
        delivery.RescheduleManually(
            DateTime.UtcNow.AddDays(10)
        );

        delivery.Reschedule(
            DateTime.UtcNow.AddDays(20)
        );

        // Assert
        delivery.HasCustomSchedule.Should().BeTrue();
    }
}
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class PlanItemTests
{
    [Fact]
    public void Should_Create_PlanItem_Correctly()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var quantity = StockQuantity.Create(2).Value!;

        // Act
        var item = new PlanItem(
            planId,
            productId,
            quantity
        );

        // Assert
        item.Id.Should().NotBeEmpty();

        item.PlanId.Should().Be(planId);

        item.ProductId.Should().Be(productId);

        item.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Should_Update_Quantity_Correctly()
    {
        // Arrange
        var item = new PlanItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            StockQuantity.Create(2).Value!
        );

        var newQuantity = StockQuantity.Create(5).Value!;

        // Act
        item.UpdateQuantity(newQuantity);

        // Assert
        item.Quantity.Should().Be(newQuantity);

        item.Quantity.Value.Should().Be(5);
    }

    [Fact]
    public void Should_Keep_Same_Id_When_Updating_Quantity()
    {
        // Arrange
        var item = new PlanItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            StockQuantity.Create(1).Value!
        );

        var originalId = item.Id;

        // Act
        item.UpdateQuantity(
            StockQuantity.Create(10).Value!
        );

        // Assert
        item.Id.Should().Be(originalId);
    }

    [Fact]
    public void Should_Keep_Same_ProductId_When_Updating_Quantity()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var item = new PlanItem(
            Guid.NewGuid(),
            productId,
            StockQuantity.Create(1).Value!
        );

        // Act
        item.UpdateQuantity(
            StockQuantity.Create(7).Value!
        );

        // Assert
        item.ProductId.Should().Be(productId);
    }

    [Fact]
    public void Should_Keep_Same_PlanId_When_Updating_Quantity()
    {
        // Arrange
        var planId = Guid.NewGuid();

        var item = new PlanItem(
            planId,
            Guid.NewGuid(),
            StockQuantity.Create(1).Value!
        );

        // Act
        item.UpdateQuantity(
            StockQuantity.Create(4).Value!
        );

        // Assert
        item.PlanId.Should().Be(planId);
    }
}
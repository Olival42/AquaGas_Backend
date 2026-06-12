using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using Xunit;

public class StockMovementTests
{
    [Fact]
    public void Create_Should_Create_StockMovement_Successfully()
    {
        var productId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var movement = StockMovement.Create(
            productId,
            StockMovementType.Entry,
            10,
            "Initial stock",
            createdBy
        );

        Assert.NotEqual(Guid.Empty, movement.Id);
        Assert.Equal(productId, movement.ProductId);
        Assert.Equal(StockMovementType.Entry, movement.Type);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal("Initial stock", movement.Reason);
        Assert.Equal(createdBy, movement.CreatedBy);
        Assert.Null(movement.ReferenceId);
        Assert.True(movement.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_Should_Trim_Reason()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "   Stock adjustment   ",
            Guid.NewGuid()
        );

        Assert.Equal("Stock adjustment", movement.Reason);
    }

    [Fact]
    public void Create_Should_Set_ReferenceId_When_Provided()
    {
        var referenceId = Guid.NewGuid();

        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Exit,
            5,
            "Sale",
            Guid.NewGuid(),
            referenceId
        );

        Assert.Equal(referenceId, movement.ReferenceId);
    }

    [Fact]
    public void Create_Should_Generate_Different_Ids()
    {
        var movement1 = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "Entry",
            Guid.NewGuid()
        );

        var movement2 = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "Entry",
            Guid.NewGuid()
        );

        Assert.NotEqual(movement1.Id, movement2.Id);
    }

    [Fact]
    public void Create_Should_Set_CreatedAt_Close_To_UtcNow()
    {
        var before = DateTime.UtcNow;

        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "Entry",
            Guid.NewGuid()
        );

        var after = DateTime.UtcNow;

        Assert.True(movement.CreatedAt >= before);
        Assert.True(movement.CreatedAt <= after);
    }

    [Fact]
    public void Create_Should_Allow_Zero_Quantity()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            0,
            "Adjustment",
            Guid.NewGuid()
        );

        Assert.Equal(0, movement.Quantity);
    }

    [Fact]
    public void Create_Should_Allow_Negative_Quantity_Because_No_Validation_Exists()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Exit,
            -10,
            "Manual correction",
            Guid.NewGuid()
        );

        Assert.Equal(-10, movement.Quantity);
    }

    [Fact]
    public void Create_Should_Allow_Empty_Reason_Because_No_Validation_Exists()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "",
            Guid.NewGuid()
        );

        Assert.Equal("", movement.Reason);
    }

    [Fact]
    public void Create_Should_Allow_Whitespace_Reason_Because_No_Validation_Exists()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(),
            StockMovementType.Entry,
            10,
            "     ",
            Guid.NewGuid()
        );

        Assert.Equal("", movement.Reason);
    }
}
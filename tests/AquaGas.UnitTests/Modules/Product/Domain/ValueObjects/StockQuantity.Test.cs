using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using Xunit;

public class StockQuantityTests
{
    [Fact]
    public void Create_Should_Return_Success_When_Quantity_Is_Valid()
    {
        var quantity = 10;

        var result = StockQuantity.Create(quantity);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Quantity_Is_Negative()
    {
        var quantity = -1;

        var result = StockQuantity.Create(quantity);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Stock quantity cannot be negative");
    }

    [Fact]
    public void Create_Should_Allow_Zero_Quantity()
    {
        var quantity = 0;

        var result = StockQuantity.Create(quantity);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Value);
    }

    [Fact]
    public void Increase_Should_Return_Success_When_Amount_Is_Valid()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Increase(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.Value);
    }

    [Fact]
    public void Increase_Should_Return_Failure_When_Amount_Is_Zero()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Increase(0);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Increase amount must be greater than zero");
    }

    [Fact]
    public void Increase_Should_Return_Failure_When_Amount_Is_Negative()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Increase(-5);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Increase amount must be greater than zero");
    }

    [Fact]
    public void Increase_Should_Handle_Large_Values()
    {
        var stock = StockQuantity.Create(1_000_000).Value!;

        var result = stock.Increase(500_000);

        Assert.True(result.IsSuccess);
        Assert.Equal(1_500_000, result.Value!.Value);
    }

    [Fact]
    public void Decrease_Should_Return_Success_When_Amount_Is_Valid()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Decrease(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Value);
    }

    [Fact]
    public void Decrease_Should_Return_Success_When_Amount_Equals_Stock()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Decrease(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Value);
    }

    [Fact]
    public void Decrease_Should_Return_Failure_When_Amount_Is_Zero()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Decrease(0);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Decrease amount must be greater than zero");
    }

    [Fact]
    public void Decrease_Should_Return_Failure_When_Amount_Is_Negative()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Decrease(-5);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Decrease amount must be greater than zero");
    }

    [Fact]
    public void Decrease_Should_Return_Failure_When_Stock_Is_Insufficient()
    {
        var stock = StockQuantity.Create(10).Value!;

        var result = stock.Decrease(11);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Message == "Insufficient stock");
    }

    [Fact]
    public void Reset_Should_Set_Quantity_To_Zero()
    {
        var stock = StockQuantity.Create(50).Value!;

        var result = stock.Reset();

        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        var stock2 = StockQuantity.Create(10).Value!;

        var result = stock1.Equals(stock2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Should_Return_False_When_Values_Are_Different()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        var stock2 = StockQuantity.Create(20).Value!;

        var result = stock1.Equals(stock2);

        Assert.False(result);
    }

    [Fact]
    public void Operator_Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        var stock2 = StockQuantity.Create(10).Value!;

        Assert.True(stock1 == stock2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Return_True_When_Values_Are_Different()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        var stock2 = StockQuantity.Create(20).Value!;

        Assert.True(stock1 != stock2);
    }

    [Fact]
    public void Operator_Equals_Should_Handle_Null_Values()
    {
        StockQuantity? stock1 = null;
        StockQuantity? stock2 = null;

        Assert.True(stock1 == stock2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Handle_One_Null_Value()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        StockQuantity? stock2 = null;

        Assert.True(stock1 != stock2);
    }

    [Fact]
    public void ToString_Should_Return_Value_As_String()
    {
        var stock = StockQuantity.Create(25).Value!;

        var result = stock.ToString();

        Assert.Equal("25", result);
    }

    [Fact]
    public void GetHashCode_Should_Be_Equal_For_Same_Values()
    {
        var stock1 = StockQuantity.Create(10).Value!;
        var stock2 = StockQuantity.Create(10).Value!;

        Assert.Equal(stock1.GetHashCode(), stock2.GetHashCode());
    }

    [Fact]
    public void Increase_Should_Not_Mutate_Original_Instance()
    {
        var original = StockQuantity.Create(10).Value!;

        var updated = original.Increase(5).Value!;

        Assert.Equal(10, original.Value);
        Assert.Equal(15, updated.Value);
    }

    [Fact]
    public void Decrease_Should_Not_Mutate_Original_Instance()
    {
        var original = StockQuantity.Create(10).Value!;

        var updated = original.Decrease(5).Value!;

        Assert.Equal(10, original.Value);
        Assert.Equal(5, updated.Value);
    }

    [Fact]
public void Equals_Object_Should_Return_True_When_Same_Value()
{
    object stock1 = StockQuantity.Create(10).Value!;
    object stock2 = StockQuantity.Create(10).Value!;

    var result = stock1.Equals(stock2);

    Assert.True(result);
}

[Fact]
public void Equals_Should_Return_False_When_Object_Is_Null()
{
    var stock = StockQuantity.Create(10).Value!;

    var result = stock.Equals(null);

    Assert.False(result);
}

[Fact]
public void Equals_Should_Return_False_When_Object_Is_Different_Type()
{
    var stock = StockQuantity.Create(10).Value!;

    var result = stock.Equals("10");

    Assert.False(result);
}

[Fact]
public void GetHashCode_Should_Be_Different_For_Different_Values()
{
    var stock1 = StockQuantity.Create(10).Value!;
    var stock2 = StockQuantity.Create(20).Value!;

    Assert.NotEqual(stock1.GetHashCode(), stock2.GetHashCode());
}
}
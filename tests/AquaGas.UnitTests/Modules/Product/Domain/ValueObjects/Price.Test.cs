using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using Xunit;

public class PriceTests
{
    [Fact]
    public void Create_Should_Return_Success_When_Price_Is_Valid()
    {
        var result = Price.Create(10.50m);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10.50m, result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Round_To_Two_Decimal_Places()
    {
        var result = Price.Create(10.567m);

        Assert.True(result.IsSuccess);
        Assert.Equal(10.57m, result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Price_Is_Zero()
    {
        var result = Price.Create(0);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Price must be greater than zero");
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Price_Is_Negative()
    {
        var result = Price.Create(-10);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors,
            e => e.Message == "Price must be greater than zero");
    }

    [Fact]
    public void Create_Should_Handle_Very_Large_Values()
    {
        var result = Price.Create(999999999.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(999999999.99m, result.Value!.Value);
    }

    [Fact]
    public void Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(10.50m).Value!;

        Assert.True(price1.Equals(price2));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Values_Are_Different()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(20.50m).Value!;

        Assert.False(price1.Equals(price2));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Object_Is_Null()
    {
        var price = Price.Create(10.50m).Value!;

        Assert.False(price.Equals(null));
    }

    [Fact]
    public void Equals_Should_Return_False_When_Object_Is_Different_Type()
    {
        var price = Price.Create(10.50m).Value!;

        Assert.False(price.Equals("10.50"));
    }

    [Fact]
    public void Operator_Equals_Should_Return_True_When_Values_Are_Equal()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(10.50m).Value!;

        Assert.True(price1 == price2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Return_True_When_Values_Are_Different()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(20.50m).Value!;

        Assert.True(price1 != price2);
    }

    [Fact]
    public void Operator_Equals_Should_Handle_Null_Values()
    {
        Price? price1 = null!;
        Price? price2 = null!;

        Assert.True(price1 == price2);
    }

    [Fact]
    public void Operator_NotEquals_Should_Handle_One_Null_Value()
    {
        var price1 = Price.Create(10.50m).Value!;
        Price? price2 = null!;

        Assert.True(price1 != price2);
    }

    [Fact]
    public void GetHashCode_Should_Be_Equal_For_Same_Values()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(10.50m).Value!;

        Assert.Equal(price1.GetHashCode(), price2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Should_Be_Different_For_Different_Values()
    {
        var price1 = Price.Create(10.50m).Value!;
        var price2 = Price.Create(20.50m).Value!;

        Assert.NotEqual(price1.GetHashCode(), price2.GetHashCode());
    }

    [Fact]
    public void ToString_Should_Return_Value_With_Two_Decimal_Places()
    {
        var price = Price.Create(10.5m).Value!;

        Assert.Equal("10.50", price.ToString());
    }

    [Fact]
    public void Rounded_Values_Should_Be_Equal()
    {
        var price1 = Price.Create(10.555m).Value!;
        var price2 = Price.Create(10.56m).Value!;

        Assert.True(price1 == price2);
    }

    [Fact]
    public void Create_Should_Round_Down_Correctly()
    {
        var result = Price.Create(10.554m);

        Assert.True(result.IsSuccess);
        Assert.Equal(10.55m, result.Value!.Value);
    }

    [Fact]
    public void Create_Should_Handle_Minimum_Valid_Value()
    {
        var result = Price.Create(0.01m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.01m, result.Value!.Value);
    }
}
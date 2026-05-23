using AquaGas.Shared.Domain.ValueObjects;
using Xunit;

public class DiscountTests
{
    [Fact]
    public void Should_Create_Discount_When_Value_Is_Valid()
    {
        var result = Discount.Create(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.Value);
    }

    [Fact]
    public void Should_Create_Discount_When_Value_Is_Zero()
    {
        var result = Discount.Create(0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Value);
    }

    [Fact]
    public void Should_Create_Discount_When_Value_Is_One_Hundred()
    {
        var result = Discount.Create(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.Value);
    }

    [Fact]
    public void Should_Fail_When_Discount_Is_Negative()
    {
        var result = Discount.Create(-1);

        Assert.True(result.IsFailure);
        Assert.Equal("Discount must be greater than 0", result.Errors[0].Message);
    }

    [Fact]
    public void Should_Fail_When_Discount_Is_Greater_Than_One_Hundred()
    {
        var result = Discount.Create(101);

        Assert.True(result.IsFailure);
        Assert.Equal("Discount cannot be greater than 100", result.Errors[0].Message);
    }

    [Fact]
    public void Should_Return_Correct_ToString()
    {
        var result = Discount.Create(15);

        Assert.True(result.IsSuccess);
        Assert.Equal("15%", result.Value!.ToString());
    }

    [Fact]
    public void Should_Return_Correct_ToString_With_Decimal_Value()
    {
        var result = Discount.Create(12.5);

        Assert.True(result.IsSuccess);
        Assert.Equal("12.5%", result.Value!.ToString());
    }

    [Fact]
    public void Discounts_With_Same_Value_Should_Be_Equal()
    {
        var discount1 = Discount.Create(20).Value;
        var discount2 = Discount.Create(20).Value;

        Assert.Equal(discount1, discount2);
    }

    [Fact]
    public void Discounts_With_Different_Values_Should_Not_Be_Equal()
    {
        var discount1 = Discount.Create(10).Value;
        var discount2 = Discount.Create(20).Value;

        Assert.NotEqual(discount1, discount2);
    }
}
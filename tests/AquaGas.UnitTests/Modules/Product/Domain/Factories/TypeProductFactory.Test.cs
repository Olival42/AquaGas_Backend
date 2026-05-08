using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Factories;
using Xunit;

public class TypeProductFactoryTests
{
    [Fact]
    public void Create_Should_Return_Water_When_Input_Is_Water()
    {
        var result = TypeProductFactory.Create("Water");

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeProduct.Water, result.Value);
    }

    [Fact]
    public void Create_Should_Return_Gas_When_Input_Is_Gas()
    {
        var result = TypeProductFactory.Create("Gas");

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeProduct.Gas, result.Value);
    }

    [Fact]
    public void Create_Should_Trim_Input()
    {
        var result = TypeProductFactory.Create("   Water   ");

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeProduct.Water, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Input_Is_Empty(string input)
    {
        var result = TypeProductFactory.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("TypeProduct is required", result.Errors.First().Message);
    }

    [Fact]
    public void Create_Should_Fail_When_Input_Is_Null()
    {
        var result = TypeProductFactory.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal("TypeProduct is required", result.Errors.First().Message);
    }

    [Theory]
    [InlineData("water")]
    [InlineData("WATER")]
    [InlineData("gas")]
    [InlineData("GAS")]
    [InlineData("Agua")]
    [InlineData("GLP")]
    [InlineData("123")]
    [InlineData("WaterGas")]
    public void Create_Should_Fail_When_Input_Is_Invalid(string input)
    {
        var result = TypeProductFactory.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Type of product is invalid. Allowed: Water, Gas",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Create_Should_Return_Error_With_Correct_Property_Name()
    {
        var result = TypeProductFactory.Create("Invalid");

        Assert.True(result.IsFailure);
        Assert.Equal("TypeProduct", result.Errors.First().Field);
    }

    [Fact]
    public void Create_Should_Return_Only_One_Error()
    {
        var result = TypeProductFactory.Create("Invalid");

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
    }
}
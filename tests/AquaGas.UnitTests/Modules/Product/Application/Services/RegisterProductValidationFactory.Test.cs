using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.API.Modules.Product.Application.Services;
using AquaGas.Api.Modules.Product.Domain.Enums;
using Xunit;

public class RegisterProductValidationFactoryTests
{
    [Fact]
    public void Combine_Should_Return_Success_When_Input_Is_Valid()
    {
        var input = new RegisterProductInput
        {
            Name = "Água Mineral",
            Type = "Water",
            Price = 10.50m,
            Quantity = 20
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("Água Mineral", result.Value!.Name.Value);
        Assert.Equal(TypeProduct.Water, result.Value.Type);
        Assert.Equal(10.50m, result.Value.Price.Value);
        Assert.Equal(20, result.Value.Quantity.Value);
    }

    [Fact]
    public void Combine_Should_Fail_When_Name_Is_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Field == "Name"
        );
    }

    [Fact]
    public void Combine_Should_Fail_When_Type_Is_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Invalid",
            Price = 10,
            Quantity = 5
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Field == "TypeProduct"
        );
    }

    [Fact]
    public void Combine_Should_Fail_When_Price_Is_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 0,
            Quantity = 5
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Field == "Price"
        );
    }

    [Fact]
    public void Combine_Should_Fail_When_Quantity_Is_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10,
            Quantity = -1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Field == "Quantity"
        );
    }

    [Fact]
    public void Combine_Should_Return_Multiple_Errors_When_Multiple_Fields_Are_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "Invalid",
            Price = 0,
            Quantity = -10
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Equal(4, result.Errors.Count);
    }

    [Fact]
    public void Combine_Should_Trim_And_Normalize_Valid_Values()
    {
        var input = new RegisterProductInput
        {
            Name = "   Água Premium   ",
            Type = "Water",
            Price = 15.999m,
            Quantity = 10
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("Água Premium", result.Value!.Name.Value);
        Assert.Equal(16.00m, result.Value.Price.Value);
    }

    [Fact]
    public void Combine_Should_Return_Only_Validation_Errors()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "",
            Price = -1,
            Quantity = -1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.All(
            result.Errors,
            error => Assert.NotEmpty(error.Message)
        );
    }

    [Fact]
    public void Combine_Should_Return_Null_Value_When_Result_Is_Failure()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "Invalid",
            Price = -1,
            Quantity = -1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Combine_Should_Accept_Quantity_Zero()
    {
        var input = new RegisterProductInput
        {
            Name = "Água",
            Type = "Water",
            Price = 10,
            Quantity = 0
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Quantity.Value);
    }

    [Fact]
    public void Combine_Should_Accept_Minimum_Name_Length()
    {
        var input = new RegisterProductInput
        {
            Name = "Gas",
            Type = "Gas",
            Price = 100,
            Quantity = 5
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("Gas", result.Value!.Name.Value);
    }

    [Fact]
    public void Combine_Should_Round_Price_To_Two_Decimal_Places()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10.999m,
            Quantity = 1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(11.00m, result.Value!.Price.Value);
    }

    [Fact]
    public void Combine_Should_Preserve_Product_Name_Casing()
    {
        var input = new RegisterProductInput
        {
            Name = "Água Premium",
            Type = "Water",
            Price = 10,
            Quantity = 1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("Água Premium", result.Value!.Name.Value);
    }

    [Fact]
    public void Combine_Should_Not_Return_Duplicate_Errors()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "",
            Price = -1,
            Quantity = -1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        var uniqueMessages = result.Errors
            .Select(e => e.Message)
            .Distinct()
            .Count();

        Assert.Equal(uniqueMessages, result.Errors.Count);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.004)]
    [InlineData(0.005)]
    [InlineData(9999999.999)]
    public void Combine_Should_Handle_Extreme_Price_Values(decimal price)
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = price,
            Quantity = 1
        };

        var result = RegisterProductValidationFactory.Combine(input);

        if (price <= 0)
            Assert.True(result.IsFailure);
        else
            Assert.True(result.IsSuccess);
    }
}
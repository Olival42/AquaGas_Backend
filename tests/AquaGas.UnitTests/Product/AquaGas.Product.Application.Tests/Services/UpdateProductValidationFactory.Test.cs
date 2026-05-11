using AquaGas.Product.Application.Services;
using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Domain.Enums;
using Xunit;

public class UpdateProductValidationFactoryTests
{
    [Fact]
    public void Combine_Should_Return_Success_When_Valid_Name_Is_Provided()
    {
        var input = new UpdateProductInput
        {
            Name = "Água Mineral"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Água Mineral", result.Value!.Name!.Value);
        Assert.Null(result.Value.Price);
        Assert.Null(result.Value.Type);
    }

    [Fact]
    public void Combine_Should_Return_Success_When_Valid_Price_Is_Provided()
    {
        var input = new UpdateProductInput
        {
            Price = 25.50m
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(25.50m, result.Value!.Price!.Value);
        Assert.Null(result.Value.Name);
        Assert.Null(result.Value.Type);
    }

    [Fact]
    public void Combine_Should_Return_Success_When_Valid_Type_Is_Provided()
    {
        var input = new UpdateProductInput
        {
            Type = "Water"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TypeProduct.Water, result.Value!.Type);
        Assert.Null(result.Value.Name);
        Assert.Null(result.Value.Price);
    }

    [Fact]
    public void Combine_Should_Return_Success_When_All_Fields_Are_Valid()
    {
        var input = new UpdateProductInput
        {
            Name = "Gás P13",
            Price = 120.90m,
            Type = "Gas"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal("Gás P13", result.Value!.Name!.Value);
        Assert.Equal(120.90m, result.Value.Price!.Value);
        Assert.Equal(TypeProduct.Gas, result.Value.Type);
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_No_Field_Is_Provided()
    {
        var input = new UpdateProductInput();

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);

        Assert.Equal(
            "At least one field must be provided",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_Name_Is_Invalid()
    {
        var input = new UpdateProductInput
        {
            Name = "A"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Product name must have at least 3 characters"
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_Price_Is_Invalid()
    {
        var input = new UpdateProductInput
        {
            Price = 0
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Price must be greater than zero"
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_Type_Is_Invalid()
    {
        var input = new UpdateProductInput
        {
            Type = "Food"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of product is invalid. Allowed: Water, Gas"
        );
    }

    [Fact]
    public void Combine_Should_Return_Multiple_Errors_When_Multiple_Fields_Are_Invalid()
    {
        var input = new UpdateProductInput
        {
            Name = "A",
            Price = -10,
            Type = "Invalid"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Equal(3, result.Errors.Count);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Product name must have at least 3 characters"
        );

        Assert.Contains(
            result.Errors,
            e => e.Message == "Price must be greater than zero"
        );

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of product is invalid. Allowed: Water, Gas"
        );
    }

    [Fact]
    public void Combine_Should_Accept_Name_With_Trimmed_Spaces()
    {
        var input = new UpdateProductInput
        {
            Name = "   Água Crystal   "
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("Água Crystal", result.Value!.Name!.Value);
    }

    [Fact]
    public void Combine_Should_Round_Price_To_Two_Decimal_Places()
    {
        var input = new UpdateProductInput
        {
            Price = 10.999m
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(11.00m, result.Value!.Price!.Value);
    }

    [Fact]
    public void Combine_Should_Be_Case_Sensitive_For_Type()
    {
        var input = new UpdateProductInput
        {
            Type = "water"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of product is invalid. Allowed: Water, Gas"
        );
    }

    [Fact]
    public void Combine_Should_Return_Error_When_Name_Is_Only_Spaces()
    {
        var input = new UpdateProductInput
        {
            Name = "     "
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Product name is required"
        );
    }

    [Fact]
    public void Combine_Should_Return_Error_When_Type_Is_Only_Spaces()
    {
        var input = new UpdateProductInput
        {
            Type = "    "
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "TypeProduct is required"
        );
    }

    [Fact]
    public void Combine_Should_Return_Error_When_Name_And_Type_Are_Invalid()
    {
        var input = new UpdateProductInput
        {
            Name = "AB",
            Type = "Invalid"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Product name must have at least 3 characters"
        );

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of product is invalid. Allowed: Water, Gas"
        );
    }

    [Fact]
    public void Combine_Should_Allow_Updating_Only_Name()
    {
        var input = new UpdateProductInput
        {
            Name = "Novo Produto"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value!.Name);
        Assert.Null(result.Value.Price);
        Assert.Null(result.Value.Type);
    }

    [Fact]
    public void Combine_Should_Allow_Updating_Only_Type()
    {
        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value!.Name);
        Assert.Null(result.Value.Price);
        Assert.Equal(TypeProduct.Gas, result.Value.Type);
    }

    [Fact]
    public void Combine_Should_Allow_Updating_Only_Price()
    {
        var input = new UpdateProductInput
        {
            Price = 99.99m
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value!.Name);
        Assert.Null(result.Value.Type);
        Assert.Equal(99.99m, result.Value.Price!.Value);
    }

    [Fact]
    public void Combine_Should_Preserve_Accented_Characters_In_Name()
    {
        var input = new UpdateProductInput
        {
            Name = "Água São Lourenço"
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Água São Lourenço",
            result.Value!.Name!.Value
        );
    }

    [Fact]
    public void Combine_Should_Return_Error_When_Price_Is_Negative()
    {
        var input = new UpdateProductInput
        {
            Price = -999.99m
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Price must be greater than zero"
        );
    }

    [Fact]
    public void Combine_Should_Return_Error_When_Price_Is_Very_Low()
    {
        var input = new UpdateProductInput
        {
            Price = 0.0001m
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.00m, result.Value!.Price!.Value);
    }

    [Fact]
    public void Combine_Should_Trim_Name_Before_Validation()
    {
        var input = new UpdateProductInput
        {
            Name = "   ABC   "
        };

        var result = UpdateProductValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("ABC", result.Value!.Name!.Value);
    }
}
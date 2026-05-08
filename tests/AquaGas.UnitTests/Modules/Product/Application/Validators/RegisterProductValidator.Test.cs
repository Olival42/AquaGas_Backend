using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.Validators;
using Xunit;

namespace AquaGas.Tests.Modules.Product.Application.Validators;

public class RegisterProductValidatorTests
{
    private readonly RegisterProductValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_Input_Is_Valid()
    {
        var input = new RegisterProductInput
        {
            Name = "Água Mineral",
            Type = "Water",
            Price = 10.50m,
            Quantity = 20
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_Should_Fail_When_Name_Is_Empty(string name)
    {
        var input = new RegisterProductInput
        {
            Name = name,
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Name"
              && e.ErrorMessage == "Name is required"
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_Should_Fail_When_Type_Is_Empty(string type)
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = type,
            Price = 10,
            Quantity = 5
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Type"
              && e.ErrorMessage == "Type is required"
        );
    }

    [Fact]
    public void Validate_Should_Pass_When_Price_Is_Zero_Because_No_Range_Rule_Exists()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 0,
            Quantity = 5
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Pass_When_Quantity_Is_Zero_Because_No_Range_Rule_Exists()
    {
        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10,
            Quantity = 0
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Return_Multiple_Errors_When_Input_Is_Invalid()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = ""
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_Should_Keep_Override_Property_Names()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "",
            Price = 10,
            Quantity = 5
        };

        var result = _validator.Validate(input);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Name"
        );

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Type"
        );
    }

    [Fact]
    public void Validate_Should_Not_Return_Error_For_Valid_Fields()
    {
        var input = new RegisterProductInput
        {
            Name = "Gás",
            Type = "Gas",
            Price = 150,
            Quantity = 10
        };

        var result = _validator.Validate(input);

        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == "Name"
        );

        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == "Type"
        );

        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == "Price"
        );

        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == "Quantity"
        );
    }
}
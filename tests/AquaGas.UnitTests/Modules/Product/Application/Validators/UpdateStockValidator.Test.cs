using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.Validators;
using Xunit;

public class UpdateStockValidatorTests
{
    private readonly UpdateStockValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_Input_Is_Valid()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_Should_Fail_When_StockMovementType_Is_Empty(string type)
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = type,
            Reason = "Restock"
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "StockMovementType"
              && e.ErrorMessage == "StockMovementType is required"
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_Should_Fail_When_Reason_Is_Empty(string reason)
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = reason
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Reason"
              && e.ErrorMessage == "Reason is required"
        );
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    public void Validate_Should_Fail_When_Reason_Has_Less_Than_3_Characters(string reason)
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = reason
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Reason"
              && e.ErrorMessage == "Reason must have at least 3 characters"
        );
    }

    [Fact]
    public void Validate_Should_Pass_When_Reason_Has_Exactly_3_Characters()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = "Gas"
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_Should_Pass_When_Quantity_Is_Zero_Or_Negative_Because_No_Rule_Exists(int quantity)
    {
        var input = new UpdateStockInput
        {
            Quantity = quantity,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Return_Multiple_Errors_When_Input_Is_Invalid()
    {
        var input = new UpdateStockInput
        {
            StockMovementType = "",
            Reason = "A"
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_Should_Keep_Override_Property_Name()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "",
            Reason = ""
        };

        var result = _validator.Validate(input);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "StockMovementType"
        );

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == "Reason"
        );
    }
}
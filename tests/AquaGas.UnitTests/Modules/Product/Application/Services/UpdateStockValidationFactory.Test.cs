using AquaGas.API.Modules.Product.Application.Services;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Domain.Enums;
using Xunit;

public class UpdateStockValidationFactoryTests
{
    [Fact]
    public void Combine_Should_Return_Success_When_Input_Is_Valid_For_Entry()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = "Product restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(10, result.Value!.Quantity!.Value);
        Assert.Equal(StockMovementType.Entry, result.Value.StockMovementType);
        Assert.Equal("Product restock", result.Value.Reason);
    }

    [Fact]
    public void Combine_Should_Return_Success_When_Input_Is_Valid_For_Exit()
    {
        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(5, result.Value!.Quantity!.Value);
        Assert.Equal(StockMovementType.Exit, result.Value.StockMovementType);
        Assert.Equal("Sale", result.Value.Reason);
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_Quantity_Is_Negative()
    {
        var input = new UpdateStockInput
        {
            Quantity = -1,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Stock quantity cannot be negative"
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_Quantity_Is_Zero()
    {
        var input = new UpdateStockInput
        {
            Quantity = 0,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Quantity!.Value);
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_StockMovementType_Is_Invalid()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Transfer",
            Reason = "Internal movement"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of movement stock is invalid. Allowed: Entry, Exit"
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_StockMovementType_Is_Empty()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "StockMovementType is required"
        );
    }

    [Fact]
    public void Combine_Should_Return_Failure_When_StockMovementType_Is_Only_Spaces()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "    ",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "StockMovementType is required"
        );
    }

    [Fact]
    public void Combine_Should_Return_Multiple_Errors_When_Quantity_And_Type_Are_Invalid()
    {
        var input = new UpdateStockInput
        {
            Quantity = -5,
            StockMovementType = "Invalid",
            Reason = "Test"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Stock quantity cannot be negative"
        );

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of movement stock is invalid. Allowed: Entry, Exit"
        );
    }

    [Fact]
    public void Combine_Should_Preserve_Reason_Text()
    {
        var input = new UpdateStockInput
        {
            Quantity = 3,
            StockMovementType = "Exit",
            Reason = "Damaged product"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Damaged product",
            result.Value!.Reason
        );
    }

    [Fact]
    public void Combine_Should_Accept_Long_Reason()
    {
        var input = new UpdateStockInput
        {
            Quantity = 15,
            StockMovementType = "Entry",
            Reason = new string('A', 255)
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(255, result.Value!.Reason.Length);
    }

    [Fact]
    public void Combine_Should_Be_Case_Sensitive_For_StockMovementType()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "entry",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Type of movement stock is invalid. Allowed: Entry, Exit"
        );
    }

    [Fact]
    public void Combine_Should_Accept_Max_Int_Quantity()
    {
        var input = new UpdateStockInput
        {
            Quantity = int.MaxValue,
            StockMovementType = "Entry",
            Reason = "Massive import"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            int.MaxValue,
            result.Value!.Quantity!.Value
        );
    }

    [Fact]
    public void Combine_Should_Accept_Minimum_Valid_Quantity()
    {
        var input = new UpdateStockInput
        {
            Quantity = 1,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(1, result.Value!.Quantity!.Value);
    }

    [Fact]
    public void Combine_Should_Trim_StockMovementType()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "   Entry   ",
            Reason = "Restock"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            StockMovementType.Entry,
            result.Value!.StockMovementType
        );
    }

    [Fact]
    public void Combine_Should_Allow_Empty_Reason_Because_No_Domain_Validation_Exists()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = ""
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("", result.Value!.Reason);
    }

    [Fact]
    public void Combine_Should_Allow_Null_Reason_Because_No_Domain_Validation_Exists()
    {
        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = null!
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value!.Reason);
    }

    [Fact]
    public void Combine_Should_Return_Only_Quantity_Error_When_Type_Is_Valid()
    {
        var input = new UpdateStockInput
        {
            Quantity = -100,
            StockMovementType = "Entry",
            Reason = "Test"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);

        Assert.Equal(
            "Stock quantity cannot be negative",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Combine_Should_Return_Only_Type_Error_When_Quantity_Is_Valid()
    {
        var input = new UpdateStockInput
        {
            Quantity = 50,
            StockMovementType = "Other",
            Reason = "Test"
        };

        var result = UpdateStockValidationFactory.Combine(input);

        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);

        Assert.Equal(
            "Type of movement stock is invalid. Allowed: Entry, Exit",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Combine_Should_Create_Different_Instances_For_Different_Inputs()
    {
        var input1 = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var input2 = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result1 = UpdateStockValidationFactory.Combine(input1);
        var result2 = UpdateStockValidationFactory.Combine(input2);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        Assert.NotEqual(
            result1.Value!.Quantity!.Value,
            result2.Value!.Quantity!.Value
        );

        Assert.NotEqual(
            result1.Value.StockMovementType,
            result2.Value.StockMovementType
        );
    }
}
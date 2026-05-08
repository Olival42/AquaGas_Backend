using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Factories;
using Xunit;

public class StockMovementTypeFactoryTests
{
    [Fact]
    public void Create_Should_Return_Entry_When_Input_Is_Entry()
    {
        var result = StockMovementTypeFactory.Create("Entry");

        Assert.True(result.IsSuccess);
        Assert.Equal(StockMovementType.Entry, result.Value);
    }

    [Fact]
    public void Create_Should_Return_Exit_When_Input_Is_Exit()
    {
        var result = StockMovementTypeFactory.Create("Exit");

        Assert.True(result.IsSuccess);
        Assert.Equal(StockMovementType.Exit, result.Value);
    }

    [Fact]
    public void Create_Should_Trim_Input()
    {
        var result = StockMovementTypeFactory.Create("   Entry   ");

        Assert.True(result.IsSuccess);
        Assert.Equal(StockMovementType.Entry, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Input_Is_Empty(string input)
    {
        var result = StockMovementTypeFactory.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "StockMovementType is required",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Create_Should_Fail_When_Input_Is_Null()
    {
        var result = StockMovementTypeFactory.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "StockMovementType is required",
            result.Errors.First().Message
        );
    }

    [Theory]
    [InlineData("entry")]
    [InlineData("ENTRY")]
    [InlineData("exit")]
    [InlineData("EXIT")]
    [InlineData("Add")]
    [InlineData("Remove")]
    [InlineData("123")]
    [InlineData("EntryExit")]
    public void Create_Should_Fail_When_Input_Is_Invalid(string input)
    {
        var result = StockMovementTypeFactory.Create(input);

        Assert.True(result.IsFailure);

        Assert.Equal(
            "Type of movement stock is invalid. Allowed: Entry, Exit",
            result.Errors.First().Message
        );
    }

    [Fact]
    public void Create_Should_Return_Error_With_Correct_Field()
    {
        var result = StockMovementTypeFactory.Create("Invalid");

        Assert.True(result.IsFailure);

        Assert.Equal(
            "StockMovementType",
            result.Errors.First().Field
        );
    }

    [Fact]
    public void Create_Should_Return_Only_One_Error()
    {
        var result = StockMovementTypeFactory.Create("Invalid");

        Assert.True(result.IsFailure);

        Assert.Single(result.Errors);
    }
}
using Xunit;
using AquaGas.Shared.Errors;

public class ErrorTests
{
    [Fact]
    public void Should_Create_Validation_Error()
    {
        var error = Error.Validation("invalid");

        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.Equal("invalid", error.Message);
    }

    [Fact]
    public void Should_Create_Unauthorized_Error()
    {
        var error = Error.Unauthorized("no access");

        Assert.Equal("UNAUTHORIZED", error.Code);
    }

    [Fact]
    public void Should_Create_NotFound_Error()
    {
        var error = Error.NotFound("missing");

        Assert.Equal("NOT_FOUND", error.Code);
    }

    [Fact]
    public void Should_Create_Conflict_Error()
    {
        var error = Error.Conflict("already exists");

        Assert.Equal("CONFLICT", error.Code);
        Assert.Equal("already exists", error.Message);
    }

    [Fact]
    public void Should_Create_InsufficientStock_Error()
    {
        var error = Error.InsufficientStock();

        Assert.Equal("INSUFFICIENT_STOCK", error.Code);
        Assert.Equal("Insufficient stock", error.Message);
    }

    [Fact]
    public void Should_Create_InsufficientStockForProduct_Error()
    {
        var productId = Guid.NewGuid();

        var error = Error.InsufficientStockForProduct(
            "Water",
            productId,
            5,
            10);

        Assert.Equal("INSUFFICIENT_STOCK", error.Code);
        Assert.Equal(
            $"Insufficient stock for product 'Water' (Id: {productId}). Available: 5, requested: 10.",
            error.Message);
    }

    [Fact]
    public void Should_Create_Forbidden_Error()
    {
        var error = Error.Forbidden("access denied");

        Assert.Equal("FORBIDDEN", error.Code);
        Assert.Equal("access denied", error.Message);
    }

    [Fact]
    public void Should_Create_Validation_Error_With_Field()
    {
        var error = Error.Validation("required field", "Name");

        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.Equal("required field", error.Message);
        Assert.Equal("Name", error.Field);
    }

    [Fact]
    public void Should_Create_Validation_Error_Without_Field()
    {
        var error = Error.Validation("invalid");

        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.Equal("invalid", error.Message);
        Assert.Null(error.Field);
    }

    [Fact]
    public void Errors_With_Same_Values_Should_Be_Equal()
    {
        var error1 = Error.Validation("invalid", "Name");
        var error2 = Error.Validation("invalid", "Name");

        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Errors_With_Different_Values_Should_Not_Be_Equal()
    {
        var error1 = Error.Validation("invalid");
        var error2 = Error.NotFound("invalid");

        Assert.NotEqual(error1, error2);
    }
}
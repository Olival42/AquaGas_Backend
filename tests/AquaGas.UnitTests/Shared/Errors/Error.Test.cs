using Xunit;
using AquaGas.Api.Shared.Errors;

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
}
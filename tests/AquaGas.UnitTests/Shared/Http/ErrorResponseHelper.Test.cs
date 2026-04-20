using Xunit;
using Microsoft.AspNetCore.Mvc;
using AquaGas.Api.Shared.Http;

public class ErrorResponseHelperMappingTests
{
    [Fact]
    public void Should_Map_Validation_Error_To_BadRequest()
    {
        var response = new { Message = "error" };

        var result = ErrorResponseHelper.ToActionResult("VALIDATION_ERROR", response);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Should_Map_Unauthorized()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "UNAUTHORIZED",
            new { });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Should_Map_NotFound()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "NOT_FOUND",
            new { });

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
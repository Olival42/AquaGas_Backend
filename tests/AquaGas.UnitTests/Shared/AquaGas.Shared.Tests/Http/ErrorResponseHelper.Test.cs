using Xunit;
using Microsoft.AspNetCore.Mvc;
using AquaGas.Shared.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

    [Fact]
    public void Should_Map_Conflict()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "CONFLICT",
            new { });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void Should_Map_Default_To_500()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "UNKNOWN",
            new { });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public void Should_Create_BadRequest_From_ModelState()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Name", "Name is required");

        var result = ErrorResponseHelper.BadRequestFromModelState(modelState);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
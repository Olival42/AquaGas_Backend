using Xunit;
using Microsoft.AspNetCore.Mvc;
using AquaGas.Shared.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using AquaGas.Shared.Responses;

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

    [Fact]
    public void Should_Map_InsufficientStock_To_Conflict()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "INSUFFICIENT_STOCK",
            new { });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void Should_Map_Forbidden_To_403()
    {
        var result = ErrorResponseHelper.ToActionResult(
            "FORBIDDEN",
            new { });

        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void Should_Return_Response_Object_In_BadRequest()
    {
        var response = new { Message = "validation error" };

        var result = ErrorResponseHelper.ToActionResult(
            "VALIDATION_ERROR",
            response);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(response, badRequest.Value);
    }

    [Fact]
    public void Should_Return_Response_Object_In_Unauthorized()
    {
        var response = new { Message = "unauthorized" };

        var result = ErrorResponseHelper.ToActionResult(
            "UNAUTHORIZED",
            response);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.Equal(response, unauthorized.Value);
    }

    [Fact]
    public void Should_Return_Response_Object_In_NotFound()
    {
        var response = new { Message = "not found" };

        var result = ErrorResponseHelper.ToActionResult(
            "NOT_FOUND",
            response);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(response, notFound.Value);
    }

    [Fact]
    public void Should_Return_Response_Object_In_Conflict()
    {
        var response = new { Message = "conflict" };

        var result = ErrorResponseHelper.ToActionResult(
            "CONFLICT",
            response);

        var conflict = Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(response, conflict.Value);
    }

    [Fact]
    public void Should_Return_Response_Object_In_Default_500()
    {
        var response = new { Message = "server error" };

        var result = ErrorResponseHelper.ToActionResult(
            "UNKNOWN",
            response);

        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal(response, objectResult.Value);
    }
}
namespace AquaGas.Api.Shared.Http;

using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ErrorResponseHelper
{
    public static BadRequestObjectResult BadRequestFromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new DataErrors(
                x.Key,
                x.Value!.Errors.Select(e => e.ErrorMessage).ToList()
            ))
            .ToList();

        var response = new ApiResponse<object?>(
            Success: false,
            Data: null,
            Error: new ErrorResponse(
                "VALIDATION_ERROR",
                "Validation failed",
                errors
            ),
            Timestamp: DateTimeOffset.UtcNow
        );

        return new BadRequestObjectResult(response);
    }

    public static IActionResult ToActionResult(string errorCode, object response)
    {
        return errorCode switch
        {
            "VALIDATION_ERROR" => new BadRequestObjectResult(response),
            "UNAUTHORIZED" => new UnauthorizedObjectResult(response),
            "NOT_FOUND" => new NotFoundObjectResult(response),
            "CONFLICT" => new ConflictObjectResult(response),
            _ => new ObjectResult(response) { StatusCode = 500 }
        };
    }
}
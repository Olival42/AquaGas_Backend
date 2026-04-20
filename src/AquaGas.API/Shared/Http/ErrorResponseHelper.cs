namespace AquaGas.Api.Shared.Http;

using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Responses;
using AquaGas.Api.Shared.Results;
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

        var response = new ApiResponse<List<DataErrors>>(
            Success: false,
            Data: errors,
            Error: null,
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
            _ => new ObjectResult(response) { StatusCode = 500 }
        };
    }
}
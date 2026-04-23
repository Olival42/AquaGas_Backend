using AquaGas.Api.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value!.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(
                new ApiResponse<object>(
                    false,
                    null,
                    new ErrorResponse(
                        "VALIDATION_ERROR",
                        "Invalid request data",
                        errors
                    ),
                    DateTimeOffset.UtcNow
                )
            );
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
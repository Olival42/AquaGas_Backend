namespace AquaGas.Api.Shared.Filters;

using AquaGas.Api.Shared.Http;
using Microsoft.AspNetCore.Mvc.Filters;

public class CustomValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        context.Result = ErrorResponseHelper.BadRequestFromModelState(context.ModelState);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
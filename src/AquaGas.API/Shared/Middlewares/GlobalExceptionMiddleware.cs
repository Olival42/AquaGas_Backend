using AquaGas.Api.Shared.Responses;

namespace AquaGas.Api.Shared.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, error) = MapException(exception);

        context.Response.StatusCode = statusCode;

        var response = new ApiResponse<object>(
            false,
            null,
            error,
            DateTimeOffset.UtcNow
        );

        return context.Response.WriteAsJsonAsync(response);
    }

    private static (int StatusCode, ErrorResponse Error) MapException(Exception ex)
    {
        return ex switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse("VALIDATION_ERROR", ex.Message)
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ErrorResponse("UNAUTHORIZED", ex.Message)
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("INTERNAL_ERROR", "Something went wrong")
            )
        };
    }
}
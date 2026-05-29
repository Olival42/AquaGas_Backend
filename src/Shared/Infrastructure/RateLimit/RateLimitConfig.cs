namespace Shared.Infrastructure.RateLimit;

using System.Security.Claims;
using System.Threading.RateLimiting;
using AquaGas.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

public static class RateLimitConfig
{
    private const int GlobalPermitLimit = 200;
    private const int AuthPermitLimit = 10;

    public static void RegisterRateLimits(this RateLimiterOptions options)
    {
        AddGlobalLimit(options);
        AddAuthLimit(options);

        options.OnRejected = async (context, info) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.Headers.RetryAfter = "60";

            var response = ApiResponse<ErrorResponse>.Fail(
                new ErrorResponse("TOO_MANY_REQUESTS", "Rate limit exceeded.")
            );

            var json = System.Text.Json.JsonSerializer.Serialize(response);

            await context.HttpContext.Response.WriteAsync(json);
        };
    }

    private static void AddGlobalLimit(this RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitPartition.GetFixedWindowLimiter(
                GetGlobalPartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = GlobalPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    }

    private static string GetGlobalPartitionKey(HttpContext context)
    {
        if (EndpointRequiresAuthentication(context) &&
            context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    private static bool EndpointRequiresAuthentication(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
            return false;

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return false;

        return endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
    }

    private static void AddAuthLimit(this RateLimiterOptions options)
    {
        options.AddPolicy("Auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = AuthPermitLimit,
                    AutoReplenishment = true,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
}
namespace AquaGas.Shared.Middlewares;

using Microsoft.AspNetCore.Http;
using AquaGas.Shared.Responses;
using System.Net;
using AquaGas.Shared.Infrastructure.TokenBlacklist;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()?
                .Replace("Bearer ", "")
                .Trim();

            if (!string.IsNullOrEmpty(token))
            {
                var isBlacklisted = await blacklistService.IsBlacklistedAsync(token);

                if (isBlacklisted)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<ErrorResponse>.Fail(
                            new ErrorResponse("UNAUTHORIZED", "Token revoked")
                        )
                    );
                    return;
                }
            }
        }

        await _next(context);
    }
}

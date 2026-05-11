namespace AquaGas.Auth.Infrastructure.Security;

using AquaGas.Shared.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

public static class JwtEventsConfig
{
    public static JwtBearerEvents GetEvents()
    {
        return new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.HttpContext.Response.StatusCode = 401;

                var message = "Invalid or missing token.";
                
                if (!string.IsNullOrEmpty(context.ErrorDescription))
                {
                    message = context.ErrorDescription;
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<ErrorResponse>.Fail(
                        new ErrorResponse("UNAUTHORIZED", message)
                    )
                );
            },

            OnForbidden = async context =>
            {
                context.HttpContext.Response.StatusCode = 403;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<ErrorResponse>.Fail(
                        new ErrorResponse("FORBIDDEN", "Access denied.")
                    )
                );
            }
        };
    }
}

namespace AquaGas.Api.Modules.Auth.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using AquaGas.Api.Modules.Auth.Application.Services;
using System.Security.Claims;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

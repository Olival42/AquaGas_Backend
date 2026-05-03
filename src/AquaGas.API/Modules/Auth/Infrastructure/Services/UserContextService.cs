using System.Security.Claims;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Modules.Auth.Application.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Result<Guid> GetUserId()
    {
        var value = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return Result<Guid>.Fail(
                Error.Unauthorized("Missing UserId in token")
            );

        if (!Guid.TryParse(value, out var userId))
            return Result<Guid>.Fail(
                Error.Unauthorized("Invalid UserId in token")
            );

        return Result<Guid>.Success(userId);
    }

    public Result<string> GetUserName()
    {
        var userName = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(userName))
            return Result<string>.Fail(
                Error.Unauthorized("Missing UserName in token")
            );

        return Result<string>.Success(userName);
    }
}
using System.Security.Claims;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Microsoft.AspNetCore.Http;
namespace AquaGas.Auth.Infrastructure.Services;

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

    public Result<Role> GetRole()
    {
        var roleValue = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(roleValue))
        {
            return Result<Role>.Fail(
                Error.Unauthorized(
                    "Missing Role in token"));
        }

        if (!Enum.TryParse<Role>(
            roleValue,
            true,
            out var role))
        {
            return Result<Role>.Fail(
                Error.Unauthorized(
                    "Invalid Role in token"));
        }

        return Result<Role>.Success(role);
    }
}
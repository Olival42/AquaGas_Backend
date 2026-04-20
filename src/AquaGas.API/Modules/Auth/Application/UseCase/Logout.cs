using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Api.Modules.Auth.Application.UseCase;

public class Logout : ILogout
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogService _audit;

    public Logout(
            IRefreshTokenService refreshTokenService,
            ITokenBlacklistService tokenBlacklistService,
            IJwtService jwtService,
            IAuditLogService audit
        )
    {
        _refreshTokenService = refreshTokenService;
        _tokenBlacklistService = tokenBlacklistService;
        _jwtService = jwtService;
        _audit = audit;
    }

    public async Task<Result> Execute(string refreshToken, string accessToken)
    {
        Guid? userId = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
                userId = _jwtService.GetUserId(accessToken);
        }
        catch
        {
            userId = null;
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var refreshTokenEntity = await _refreshTokenService.GetAsync(refreshToken);

            if (refreshTokenEntity != null)
            {
                if (userId.HasValue && refreshTokenEntity.UserId != userId.Value)
                    return Result.Fail(Error.Unauthorized("Invalid logout request"));

                refreshTokenEntity.SetRevoked();
                await _refreshTokenService.UpdateAsync(refreshTokenEntity);
            }
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            var expiresAt = _jwtService.GetTokenExpiration(accessToken);

            if (expiresAt > DateTime.UtcNow)
                await _tokenBlacklistService.AddAsync(accessToken, expiresAt);
        }

        if (userId.HasValue)
        {
            await _audit.LogAsync(
                userId,
                null,
                AuditAction.LOGOUT,
                "User",
                userId
            );
        }

        return Result.Success();
    }
}

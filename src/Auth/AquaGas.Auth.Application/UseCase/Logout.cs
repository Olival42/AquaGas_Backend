using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Shared.Results;
using Microsoft.Extensions.Logging;

namespace AquaGas.Auth.Application.UseCase;

public class Logout : ILogout
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogService _audit;
    private readonly ILogger<Logout> _logger;

    public Logout(
            IRefreshTokenService refreshTokenService,
            ITokenBlacklistService tokenBlacklistService,
            IJwtService jwtService,
            IAuditLogService audit,
            ILogger<Logout> logger
        )
    {
        _refreshTokenService = refreshTokenService;
        _tokenBlacklistService = tokenBlacklistService;
        _jwtService = jwtService;
        _audit = audit;
        _logger = logger;
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
            try
            {
                await _audit.LogAsync(
                    userId,
                    null,
                    AuditAction.LOGOUT,
                    "User",
                    userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to write audit log for user {UserId}",
                    userId
                );
            }
        }

        return Result.Success();
    }
}

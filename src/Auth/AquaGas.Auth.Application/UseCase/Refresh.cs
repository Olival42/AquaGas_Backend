using AquaGas.Application.Services;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Auth.Application.UseCase;

public class Refresh : IRefresh
{
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _audit;

    public Refresh(
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IUserRepository userRepository,
            IAuditLogService audit
        )
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
        _refreshTokenService = refreshTokenService;
        _audit = audit;
    }

    public async Task<Result<RefreshResult>> Execute(string refreshToken)
    {
        var refreshTokenEntity = await _refreshTokenService.GetAsync(refreshToken);

        if (refreshTokenEntity == null)
        {
            return Result<RefreshResult>.Fail(
                Error.Unauthorized("Invalid refresh token"));
        }

        if (refreshTokenEntity.IsRevoked)
        {
            return Result<RefreshResult>.Fail(
                Error.Unauthorized("Refresh token already used"));
        }

        if (refreshTokenEntity.IsExpired())
        {
            return Result<RefreshResult>.Fail(
                Error.Unauthorized("Refresh token expired"));
        }

        var user = await _userRepository.GetByIdAsync(refreshTokenEntity.UserId);

        if (user == null)
        {
            return Result<RefreshResult>.Fail(
                Error.NotFound("User not found"));
        }

        refreshTokenEntity.SetRevoked();
        await _refreshTokenService.UpdateAsync(refreshTokenEntity);

        var accessTokenGenerated = _jwtService.GenerateAccessToken(user);

        var refreshTokenGenerated = _jwtService.GenerateRefreshToken(user);

        await _refreshTokenService.StoreAsync(
            user,
            refreshTokenGenerated.Token,
            refreshTokenGenerated.ExpiresAt
        );

        await _audit.LogAsync(
            user.Id,
            user.UserName.Value,
            AuditAction.REFRESH_TOKEN,
            "User",
            user.Id,
            new { oldRefreshToken = refreshToken },
            new { newRefreshToken = refreshTokenGenerated }
        );

        var tokens = new TokenDto(
            accessTokenGenerated.Token,
            refreshTokenGenerated.Token,
            refreshTokenGenerated.ExpiresAt,
            new DateTimeOffset(accessTokenGenerated.ExpiresAt).ToUnixTimeSeconds()
        );

        var result = new RefreshResult(tokens);

        return Result<RefreshResult>.Success(result);
    }
}
using AquaGas.Application.Services;
using AquaGas.Auth.Application.Dtos.Requests;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Application.Services;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Mapster;
using Microsoft.Extensions.Logging;

namespace AquaGas.Auth.Application.UseCase;

public class Login : ILogin
{
    private readonly IJwtService _jwtService;
    private readonly IAuthService _authService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuditLogService _audit;
    private readonly ILogger<Login> _logger;

    public Login(
            IJwtService jwtService,
            IAuthService authService,
            IRefreshTokenService refreshTokenService,
            IAuditLogService audit,
             ILogger<Login> logger
        )
    {
        _jwtService = jwtService;
        _authService = authService;
        _refreshTokenService = refreshTokenService;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<LoginResult>> Execute(LoginInput data)
    {
        if (string.IsNullOrWhiteSpace(data.UserName) ||
            string.IsNullOrWhiteSpace(data.Password))
        {
            return Result<LoginResult>.Fail(Error.Validation("Invalid input"));
        }

        var user = await _authService.Authenticate(data.UserName, data.Password);

        if (user is null)
        {
            return Result<LoginResult>.Fail(
                Error.Unauthorized("Invalid credentials")
            );
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user);

        try
        {
            await _refreshTokenService.StoreAsync(
                user,
                refreshToken.Token,
                refreshToken.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to store refresh token for user {UserId}",
                user.Id
            );
        }

        try
        {
            await _audit.LogAsync(
                user.Id,
                user.UserName.Value,
                AuditAction.LOGIN,
                "User",
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to write audit log for user {UserId}",
                user.Id
            );
        }

        var tokens = new TokenDto(
            accessToken.Token,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            new DateTimeOffset(accessToken.ExpiresAt).ToUnixTimeSeconds()
        );

        return Result<LoginResult>.Success(
            new LoginResult(
                user.Adapt<UserResponse>(),
                tokens
            )
        );
    }
}
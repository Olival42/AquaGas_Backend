using Xunit;
using Moq;
using AquaGas.Auth.Application.UseCase;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Models;
using AquaGas.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Application.Services;
using Microsoft.Extensions.Logging;

public class LogoutUseCaseTests
{
    private readonly Mock<IRefreshTokenService> _refreshMock = new();
    private readonly Mock<ITokenBlacklistService> _blacklistMock = new();
    private readonly Mock<IJwtService> _jwtMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<ILogger<Logout>> _logger = new();

    private Logout CreateUseCase()
        => new Logout(
            _refreshMock.Object,
            _blacklistMock.Object,
            _jwtMock.Object,
            _auditMock.Object,
            _logger.Object
        );

    private Mock<IJwtService> JwtMock(Guid userId)
    {
        var jwt = new Mock<IJwtService>();

        jwt.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns(userId);

        jwt.Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.UtcNow.AddMinutes(10));

        return jwt;
    }

    [Fact]
    public async Task Should_Logout_Successfully()
    {
        var userId = Guid.NewGuid();

        _jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns(userId);

        _jwtMock.Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.UtcNow.AddMinutes(10));

        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken(userId, "hash", DateTime.UtcNow.AddDays(1)));

        var useCase = CreateUseCase();

        var result = await useCase.Execute("refresh", "access");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Revoke_Refresh_Token()
    {
        var userId = Guid.NewGuid();

        var token = new RefreshToken(userId, "hash", DateTime.UtcNow.AddDays(1));

        var refreshMock = new Mock<IRefreshTokenService>();
        refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        var jwtMock = JwtMock(userId);
        var auditMock = new Mock<IAuditLogService>();
        var blacklistMock = new Mock<ITokenBlacklistService>();

        var useCase = new Logout(refreshMock.Object, blacklistMock.Object, jwtMock.Object, auditMock.Object, _logger.Object);

        await useCase.Execute("refresh", "access");

        refreshMock.Verify(x => x.UpdateAsync(
            It.Is<RefreshToken>(t => t.IsRevoked)
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_User_Mismatch()
    {
        var jwtMock = new Mock<IJwtService>();

        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns(userId);

        var token = new RefreshToken(
            anotherUserId,
            "hash",
            DateTime.UtcNow.AddDays(1)
        );

        var refreshMock = new Mock<IRefreshTokenService>();
        refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        var useCase = new Logout(refreshMock.Object,
            new Mock<ITokenBlacklistService>().Object,
            jwtMock.Object,
            new Mock<IAuditLogService>().Object,
            _logger.Object
        );

        var result = await useCase.Execute("refresh", "access");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Add_Access_Token_To_Blacklist()
    {
        var userId = Guid.NewGuid();

        _jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns(userId);

        _jwtMock.Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.UtcNow.AddMinutes(10));

        var useCase = CreateUseCase();

        await useCase.Execute("refresh", "access");

        _blacklistMock.Verify(x =>
            x.AddAsync(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Not_Blacklist_Expired_Token()
    {
        _jwtMock.Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.UtcNow.AddMinutes(-10));

        var useCase = CreateUseCase();

        await useCase.Execute("refresh", "access");

        _blacklistMock.Verify(x =>
            x.AddAsync(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        var userId = Guid.NewGuid();

        _jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns(userId);

        var useCase = CreateUseCase();

        await useCase.Execute("refresh", "access");

        _auditMock.Verify(x =>
            x.LogAsync(
                userId,
                null,
                AuditAction.LOGOUT,
                "User",
                userId
            ),
            Times.Once);
    }

    [Fact]
    public async Task Should_Work_Without_Tokens()
    {
        var useCase = CreateUseCase();

        var result = await useCase.Execute("", "");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Handle_Invalid_Access_Token_Gracefully()
    {
        var jwtMock = new Mock<IJwtService>();

        jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Throws(new Exception("Invalid token"));

        var useCase = new Logout(
            _refreshMock.Object,
            _blacklistMock.Object,
            jwtMock.Object,
            _auditMock.Object,
            _logger.Object
        );

        var result = await useCase.Execute("refresh", "bad_token");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Handle_Already_Revoked_Refresh_Token()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1));
        token.SetRevoked();

        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        var useCase = CreateUseCase();

        var result = await useCase.Execute("refresh", "access");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Blacklist_When_Token_Has_No_Expiration()
    {
        _jwtMock.Setup(x => x.GetTokenExpiration(It.IsAny<string>()))
            .Returns(DateTime.MinValue);

        var useCase = CreateUseCase();

        await useCase.Execute("refresh", "access");

        _blacklistMock.Verify(x =>
            x.AddAsync(It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Fail_When_UserId_Is_Null()
    {
        _jwtMock.Setup(x => x.GetUserId(It.IsAny<string>()))
            .Returns((Guid?)null);

        var useCase = CreateUseCase();

        var result = await useCase.Execute("refresh", "access");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Fail_When_Audit_Fails()
    {
        _auditMock.Setup(x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ))
            .ThrowsAsync(new Exception("Audit failed"));

        var useCase = CreateUseCase();

        var result = await useCase.Execute("refresh", "access");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Fail_When_Blacklist_Fails()
    {
        _blacklistMock.Setup(x => x.AddAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Redis down"));

        var useCase = CreateUseCase();

        var result = await useCase.Execute("refresh", "access");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Be_Idempotent()
    {
        var useCase = CreateUseCase();

        var result1 = await useCase.Execute("refresh", "access");
        var result2 = await useCase.Execute("refresh", "access");

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
    }
}
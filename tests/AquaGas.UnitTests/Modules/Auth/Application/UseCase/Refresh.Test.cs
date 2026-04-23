using Xunit;
using Moq;
using AquaGas.Api.Modules.Auth.Application.UseCase;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.API.Shared.Application.Services;

public class RefreshUseCaseTests
{
    private readonly Mock<IJwtService> _jwtMock = new();
    private readonly Mock<IRefreshTokenService> _refreshMock = new();
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();

    private Refresh CreateUseCase()
        => new Refresh(
            _jwtMock.Object,
            _refreshMock.Object,
            _userMock.Object,
            _auditMock.Object
        );

    private User CreateUser()
        => new(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

    private void SetupValidFlow(User user)
    {
        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken(user.Id, "hash", DateTime.UtcNow.AddDays(1)));

        _userMock.Setup(x => x.GetById(user.Id))
            .ReturnsAsync(user);

        _jwtMock.Setup(x => x.GenerateAccessToken(user))
            .Returns(new TokenResult("access", DateTime.UtcNow.AddMinutes(10)));

        _jwtMock.Setup(x => x.GenerateRefreshToken(user))
            .Returns(new TokenResult("refresh", DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public async Task Should_Fail_When_Token_Is_Invalid()
    {
        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateUseCase().Execute("invalid");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Token_Is_Revoked()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1));
        token.SetRevoked();

        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        var result = await CreateUseCase().Execute("token");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Token_Is_Expired()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddSeconds(-10));

        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        var result = await CreateUseCase().Execute("token");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_User_Not_Found()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1));

        _refreshMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(token);

        _userMock.Setup(x => x.GetById(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await CreateUseCase().Execute("token");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Succeed_When_Valid()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        var result = await CreateUseCase().Execute("token");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access", result.Value.Tokens.AccessToken);
        Assert.Equal("refresh", result.Value.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Should_Revoke_Old_Token()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        await CreateUseCase().Execute("token");

        _refreshMock.Verify(x =>
            x.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked)),
            Times.Once);
    }

    [Fact]
    public async Task Should_Store_New_Refresh_Token()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        await CreateUseCase().Execute("token");

        _refreshMock.Verify(x =>
            x.StoreAsync(user, "refresh", It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Call_Audit_Log()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        await CreateUseCase().Execute("token");

        _auditMock.Verify(x =>
            x.LogAsync(
                user.Id,
                user.UserName.Value,
                AuditAction.REFRESH_TOKEN,
                "User",
                user.Id,
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_Success_Result()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        var result = await CreateUseCase().Execute("token");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
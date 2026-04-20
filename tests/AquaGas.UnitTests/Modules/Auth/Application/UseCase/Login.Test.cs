using Xunit;
using Moq;
using AquaGas.Api.Modules.Auth.Application.UseCase;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using Microsoft.Extensions.Logging;

public class LoginUseCaseTests
{
    private readonly Mock<IJwtService> _jwtMock = new();
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<IRefreshTokenService> _refreshMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<ILogger<Login>> _logger = new();

    private Login CreateUseCase()
        => new Login(
            _jwtMock.Object,
            _authMock.Object,
            _refreshMock.Object,
            _auditMock.Object,
            _logger.Object
        );

    private User CreateUser()
        => new(
            UserName.Create("john123").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

    private static readonly DateTime FixedAccessExpiration = DateTime.UtcNow.AddMinutes(10);
    private static readonly DateTime FixedRefreshExpiration = DateTime.UtcNow.AddDays(7);

    [Fact]
    public async Task Should_Login_Successfully()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        var result = await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.User);
        Assert.NotNull(result.Value.Tokens);
        Assert.NotNull(result.Value.Tokens.AccessToken);
        Assert.NotNull(result.Value.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Should_Fail_When_Invalid_Credentials()
    {
        _authMock.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var result = await CreateUseCase().Execute(new LoginInput
        {
            UserName = "wrong",
            Password = "wrong"
        });

        Assert.False(result.IsSuccess);

        _jwtMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        _jwtMock.Verify(x => x.GenerateRefreshToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Should_Generate_Access_Token()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        _jwtMock.Verify(x => x.GenerateAccessToken(user), Times.Once);
    }

    [Fact]
    public async Task Should_Generate_Refresh_Token()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        _jwtMock.Verify(x => x.GenerateRefreshToken(user), Times.Once);
    }

    [Fact]
    public async Task Should_Store_Refresh_Token()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        _refreshMock.Verify(x =>
            x.StoreAsync(
                user,
                "refresh",
                It.IsAny<DateTime>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task Should_Emit_Audit_Login_Event()
    {
        var user = CreateUser();
        SetupValidFlow(user);

        await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        _auditMock.Verify(x =>
            x.LogAsync(
                user.Id,
                user.UserName.Value,
                AuditAction.LOGIN,
                "User",
                user.Id
            ),
            Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Tokens_When_Failed()
    {
        _authMock.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await CreateUseCase().Execute(new LoginInput
        {
            UserName = "x",
            Password = "x"
        });

        _jwtMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        _jwtMock.Verify(x => x.GenerateRefreshToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Should_Handle_RefreshToken_Failure_Gracefully()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        _refreshMock.Setup(x =>
            x.StoreAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("redis down"));

        var result = await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Handle_Audit_Failure_Gracefully()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        _auditMock.Setup(x =>
            x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ))
            .ThrowsAsync(new Exception("audit error"));

        var result = await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Map_User_To_Response_Correctly()
    {
        var user = CreateUser();

        SetupValidFlow(user);

        var result = await CreateUseCase().Execute(new LoginInput
        {
            UserName = "john",
            Password = "123"
        });

        Assert.NotNull(result.Value!.User);
        Assert.Equal(user.UserName.Value, result.Value.User.UserName);
    }

    [Fact]
    public async Task Should_Generate_Different_Tokens_For_Different_Users()
    {
        var user1 = CreateUser();
        var user2 = new User(
            UserName.Create("user2").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

        _authMock.SetupSequence(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user1)
            .ReturnsAsync(user2);

        _jwtMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns((User u) => new TokenResult(
                $"access-{u.Id}",
                FixedAccessExpiration
            ));

        _jwtMock.Setup(x => x.GenerateRefreshToken(It.IsAny<User>()))
            .Returns((User u) => new TokenResult(
                $"refresh-{u.Id}",
                FixedRefreshExpiration
            ));

        var useCase = CreateUseCase();

        var result1 = await useCase.Execute(new LoginInput { UserName = "u1", Password = "p1" });
        var result2 = await useCase.Execute(new LoginInput { UserName = "u2", Password = "p2" });

        Assert.NotEqual(
            result1.Value!.Tokens.AccessToken,
            result2.Value!.Tokens.AccessToken
        );
    }

    [Fact]
    public async Task Should_Fail_When_Input_Is_Empty()
    {
        var useCase = CreateUseCase();

        var result1 = await useCase.Execute(new LoginInput
        {
            UserName = "",
            Password = "123"
        });

        var result2 = await useCase.Execute(new LoginInput
        {
            UserName = "user",
            Password = ""
        });

        Assert.False(result1.IsSuccess);
        Assert.False(result2.IsSuccess);

        _authMock.Verify(x =>
            x.Authenticate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    private void SetupValidFlow(User user)
    {
        _authMock.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwtMock.Setup(x => x.GenerateAccessToken(user))
            .Returns(new TokenResult("access", FixedAccessExpiration));

        _jwtMock.Setup(x => x.GenerateRefreshToken(user))
            .Returns(new TokenResult("refresh", FixedRefreshExpiration));
    }
}
using Xunit;
using Moq;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();

    private AuthService CreateService()
        => new AuthService(_repoMock.Object, _hasherMock.Object);

    private User CreateUser()
        => new(
            UserName.Create("john").Value!,
            "hashed-password",
            Role.MANAGER,
            Guid.NewGuid()
        );

    [Fact]
    public async Task Should_Return_User_When_Credentials_Are_Valid()
    {
        var user = CreateUser();

        _repoMock.Setup(x => x.GetByUserNameAsync("john"))
            .ReturnsAsync(user);

        _hasherMock.Setup(x => x.Verify(user.PasswordHash, "123"))
            .Returns(true);

        var result = await CreateService().Authenticate("john", "123");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Not_Found()
    {
        _repoMock.Setup(x => x.GetByUserNameAsync("john"))
            .ReturnsAsync((User?)null);

        var result = await CreateService().Authenticate("john", "123");

        Assert.Null(result);

        _hasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Null_When_Password_Is_Invalid()
    {
        var user = CreateUser();

        _repoMock.Setup(x => x.GetByUserNameAsync("john"))
            .ReturnsAsync(user);

        _hasherMock.Setup(x => x.Verify(user.PasswordHash, "wrong"))
            .Returns(false);

        var result = await CreateService().Authenticate("john", "wrong");

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Call_PasswordHasher()
    {
        var user = CreateUser();

        _repoMock.Setup(x => x.GetByUserNameAsync("john"))
            .ReturnsAsync(user);

        _hasherMock.Setup(x => x.Verify(user.PasswordHash, "123"))
            .Returns(true);

        await CreateService().Authenticate("john", "123");

        _hasherMock.Verify(
            x => x.Verify(user.PasswordHash, "123"),
            Times.Once
        );
    }
}
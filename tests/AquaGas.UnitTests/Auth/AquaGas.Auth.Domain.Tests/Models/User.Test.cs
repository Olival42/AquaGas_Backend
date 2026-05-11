using Xunit;
using Moq;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Auth.Domain.Services;

public class UserTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();

    [Fact]
    public void Should_Create_User_As_Active()
    {
        var user = CreateUser();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Should_Set_CreatedAt()
    {
        var user = CreateUser();

        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Should_Deactivate_User()
    {
        var user = CreateUser();

        user.Deactive();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Should_Activate_User()
    {
        var user = CreateUser();

        user.Deactive();
        user.Reactive();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Should_Change_UserName_When_Different()
    {
        var user = CreateUser();

        var changed = user.ChangeUserName(UserName.Create("maria").Value!);

        Assert.True(changed);
        Assert.Equal("maria", user.UserName.Value);
    }

    [Fact]
    public void Should_Not_Change_UserName_When_Same()
    {
        var user = CreateUser();

        var changed = user.ChangeUserName(UserName.Create("john123").Value!);

        Assert.False(changed);
    }

    [Fact]
    public void Should_Not_Change_UserName_When_Null()
    {
        var user = CreateUser();

        var changed = user.ChangeUserName(null);

        Assert.False(changed);
    }

    [Fact]
    public void Should_Change_Role_When_Different()
    {
        var user = CreateUser(Role.Employee);

        var changed = user.ChangeRole(Role.Manager);

        Assert.True(changed);
        Assert.Equal(Role.Manager, user.Role);
    }

    [Fact]
    public void Should_Not_Change_Role_When_Same()
    {
        var user = CreateUser(Role.Manager);

        var changed = user.ChangeRole(Role.Manager);

        Assert.False(changed);
    }

    [Fact]
    public void Should_Not_Change_Role_When_Null()
    {
        var user = CreateUser();

        var changed = user.ChangeRole(null);

        Assert.False(changed);
    }

    [Fact]
    public void Should_Change_Password_When_Different()
    {
        var user = CreateUser();

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns("new-hash");

        var password = Password.Create("Senha@123").Value!;

        var changed = user.ChangePassword(password, _hasher.Object);

        Assert.True(changed);
        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void Should_Not_Change_Password_When_Same()
    {
        var user = CreateUser();

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
               .Returns("same-hash");

        user = new User(
            UserName.Create("john123").Value!,
            "same-hash",
            Role.Manager,
            Guid.NewGuid()
        );

        var password = Password.Create("Senha@123").Value!;

        var changed = user.ChangePassword(password, _hasher.Object);

        Assert.False(changed);
    }

    [Fact]
    public void Should_Not_Change_Password_When_Null()
    {
        var user = CreateUser();

        var changed = user.ChangePassword(null, _hasher.Object);

        Assert.False(changed);
    }

    private User CreateUser(Role role = Role.Manager)
    {
        return new User(
            UserName.Create("john123").Value!,
            "old-hash",
            role,
            Guid.NewGuid()
        );
    }
}
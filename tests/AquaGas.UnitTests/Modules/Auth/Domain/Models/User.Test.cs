using Xunit;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class UserTests
{
    [Fact]
    public void Should_Create_User_As_Active()
    {
        var username = UserName.Create("john123").Value!;

        var user = new User(
            username,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Should_Set_CreatedAt()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Should_Deactivate_User()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        user.Deactive();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Should_Activate_User()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        user.Deactive();
        user.Active();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Should_Update_Password()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "old_hash",
            Role.Manager,
            Guid.NewGuid()
        );

        user.UpdatePassword("new_hash");

        Assert.Equal("new_hash", user.PasswordHash);
    }

    [Fact]
    public void Should_Update_Role()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        user.UpdateRole(Role.Employee);

        Assert.Equal(Role.Employee, user.Role);
    }

    [Fact]
    public void Should_Change_Role_From_Employee_To_Manager()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Employee,
            Guid.NewGuid()
        );

        user.UpdateRole(Role.Manager);

        Assert.Equal(Role.Manager, user.Role);
    }

    [Fact]
    public void Should_Keep_Role_When_Same_Role_Is_Set()
    {
        var user = new User(
            UserName.Create("john123").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

        user.UpdateRole(Role.Manager);

        Assert.Equal(Role.Manager, user.Role);
    }
}
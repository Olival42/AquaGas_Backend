using Xunit;
using Mapster;
using AquaGas.API.Modules.Auth.Application.Mappings;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public class UserMappingTests
{
    public UserMappingTests()
    {
        UserMapping.Register();
    }

    private User CreateUser()
        => new(
            UserName.Create("john").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

    [Fact]
    public void Should_Map_User_To_UserResponse()
    {
        var user = CreateUser();

        var response = user.Adapt<UserResponse>();

        Assert.NotNull(response);
        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(user.UserName.Value, response.UserName);
        Assert.Equal(user.Role, response.Role);
    }
}
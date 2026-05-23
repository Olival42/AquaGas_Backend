using Xunit;
using Mapster;
using AquaGas.Auth.Application.Mappings;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Auth.Application.Dtos.Responses;

public class UserMappingTests
{
    private readonly TypeAdapterConfig _config;

    public UserMappingTests()
    {
        _config = new TypeAdapterConfig();
        UserMapping.Register(_config);
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

        var response = user.Adapt<UserResponse>(_config);

        Assert.NotNull(response);
        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(user.UserName.Value, response.UserName);
        Assert.Equal(user.Role, response.Role);
    }
}

using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using AquaGas.Api.Modules.Auth.Infrastructure.Security.Services;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using System;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class JwtServiceTests
{
    private JwtService CreateService()
    {
        var configMock = new Mock<IConfiguration>();

        configMock.Setup(x => x["Jwt:Key"])
            .Returns("THIS_IS_A_SUPER_SECRET_KEY_FOR_UNIT_TESTS_1234567890");

        configMock.Setup(x => x["Jwt:Issuer"]).Returns("test");
        configMock.Setup(x => x["Jwt:Audience"]).Returns("test");

        return new JwtService(configMock.Object);
    }

    [Fact]
    public void Should_Generate_Access_Token_With_Claims()
    {
        var service = CreateService();

        var user = new User(
            UserName.Create("john").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

        var (token, expiresAt) = service.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.InRange(
            expiresAt,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(35)
        );
    }

    [Fact]
    public void Should_Generate_Refresh_Token()
    {
        var service = CreateService();

        var user = new User(
            UserName.Create("john").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

        var (token, expiresAt) = service.GenerateRefreshToken(user);

        Assert.NotNull(token);
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Should_Extract_UserId_From_Token()
    {
        var service = CreateService();

        var user = new User(
            UserName.Create("john").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

        var (token, _) = service.GenerateAccessToken(user);

        var userId = service.GetUserId(token);

        Assert.True(userId.HasValue);
        Assert.Equal(user.Id, userId.Value);
    }

    [Fact]
    public void Should_Get_Token_Expiration()
    {
        var service = CreateService();

        var user = new User(
            UserName.Create("john").Value!,
            "hash",
            Role.MANAGER,
            Guid.NewGuid()
        );

        var (token, _) = service.GenerateAccessToken(user);

        var exp = service.GetTokenExpiration(token);

        Assert.InRange(
            exp,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(35)
        );
    }
}
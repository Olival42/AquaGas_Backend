using Xunit;
using AquaGas.Auth.Domain.Models;
using System;

public class RefreshTokenTests
{
    [Fact]
    public void Should_Create_RefreshToken()
    {
        var userId = Guid.NewGuid();
        var expiration = DateTime.UtcNow.AddDays(7);

        var token = new RefreshToken(
            userId,
            "hash",
            expiration
        );

        Assert.Equal(userId, token.UserId);
        Assert.Equal("hash", token.TokenHash);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void Should_Revoke_Token()
    {
        var token = new RefreshToken(
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddDays(7)
        );

        token.SetRevoked();

        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void Should_Not_Override_RevokedAt()
    {
        var token = new RefreshToken(
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddDays(7)
        );

        token.SetRevoked();
        var first = token.RevokedAt;

        token.SetRevoked();

        Assert.Equal(first, token.RevokedAt);
    }

    [Fact]
    public void Should_Be_Expired()
    {
        var token = new RefreshToken(
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddSeconds(-1)
        );

        Assert.True(token.IsExpired());
    }

    [Fact]
    public void Should_Not_Be_Expired()
    {
        var token = new RefreshToken(
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddMinutes(10)
        );

        Assert.False(token.IsExpired());
    }
}
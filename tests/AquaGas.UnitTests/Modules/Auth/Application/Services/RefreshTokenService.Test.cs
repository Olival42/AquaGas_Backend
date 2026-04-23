using Xunit;
using Moq;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repoMock = new();

    private RefreshTokenService CreateService()
        => new RefreshTokenService(_repoMock.Object);

    private User CreateUser()
        => new(
            UserName.Create("john").Value!,
            "hash",
            Role.Manager,
            Guid.NewGuid()
        );

    [Fact]
    public async Task Should_Store_RefreshToken()
    {
        var user = CreateUser();
        var token = "refresh-token";
        var expires = DateTime.UtcNow.AddDays(7);

        await CreateService().StoreAsync(user, token, expires);

        _repoMock.Verify(x =>
            x.AddAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == user.Id &&
                rt.ExpirationDate == expires
            )),
            Times.Once);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Get_RefreshToken_By_Hash()
    {
        var token = "refresh-token";
        var hashed = AquaGas.Api.Shared.Security.TokenHasher.Hash(token);

        _repoMock.Setup(x => x.GetByTokenHashAsync(hashed))
            .ReturnsAsync(new RefreshToken(Guid.NewGuid(), hashed, DateTime.UtcNow));

        var result = await CreateService().GetAsync(token);

        Assert.NotNull(result);

        _repoMock.Verify(x =>
            x.GetByTokenHashAsync(hashed),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_Null_When_Token_Not_Found()
    {
        var token = "invalid";

        _repoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().GetAsync(token);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Update_RefreshToken()
    {
        var refresh = new RefreshToken(
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow
        );

        await CreateService().UpdateAsync(refresh);

        _repoMock.Verify(x => x.UpdateAsync(refresh), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
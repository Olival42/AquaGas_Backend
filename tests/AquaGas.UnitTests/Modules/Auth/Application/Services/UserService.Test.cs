using Xunit;
using Moq;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();

    private UserService CreateService()
        => new UserService(_repoMock.Object);

    private User CreateUser(Guid id)
        => new(
            UserName.Create("john").Value!,
            "hash",
            Role.MANAGER,
            id
        );

    [Fact]
    public async Task Should_Return_User_When_Found()
    {
        var id = Guid.NewGuid();
        var user = CreateUser(id);

        _repoMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(user);

        var result = await CreateService().GetById(id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
    }

    [Fact]
    public async Task Should_Return_Null_When_User_Not_Found()
    {
        var id = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((User?)null);

        var result = await CreateService().GetById(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Call_Repository()
    {
        var id = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((User?)null);

        await CreateService().GetById(id);

        _repoMock.Verify(
            x => x.GetByIdAsync(id),
            Times.Once
        );
    }
}
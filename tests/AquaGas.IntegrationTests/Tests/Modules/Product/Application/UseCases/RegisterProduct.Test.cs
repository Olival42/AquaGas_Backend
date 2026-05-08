using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Results;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Mapster;
using Moq;
using Microsoft.EntityFrameworkCore;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.Api.Shared.Errors;


public class RegisterProductTests : BaseIntegrationTest
{
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<IUserContextService> _userMock = new();

    public RegisterProductTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("test-user"));
    }

    [Fact]
    public async Task Should_Create_New_Product_When_Not_Exists()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Crystal",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            var result = await useCase.Execute(input);

            result.IsSuccess.Should().BeTrue();
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(x => x.NormalizedName == "AGUA CRYSTAL"));

        saved.Should().NotBeNull();
        saved!.IsActive.Should().BeTrue();
        saved.Quantity.Value.Should().Be(5);
    }

    [Fact]
    public async Task Should_Fail_When_Product_Already_Exists_And_IsActive()
    {
        await SeedProduct("Agua Crystal", isActive: true);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Crystal",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            var result = await useCase.Execute(input);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Reactivate_Product_When_Inactive()
    {
        var productId = await SeedProduct("Agua Crystal", isActive: false);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Crystal",
                Type = TypeProduct.Water.ToString(),
                Price = 20,
                Quantity = 10
            };

            var result = await useCase.Execute(input);

            result.IsSuccess.Should().BeTrue();
        });

        var updated = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        updated.IsActive.Should().BeTrue();
        updated.Quantity.Value.Should().Be(10);
    }

    [Fact]
    public async Task Should_Call_Audit_On_Create()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Crystal",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            await useCase.Execute(input);
        });

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.CREATE,
            "Product",
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Input_Is_Invalid()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "",
                Type = "",
                Price = 0,
                Quantity = 0
            };

            var result = await useCase.Execute(input);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Is_Invalid()
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("No user")));

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua X",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            var result = await useCase.Execute(input);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Call_Update_Audit_When_Reactivating_Product()
    {
        await SeedProduct("Agua Crystal", isActive: false);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Crystal",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            await useCase.Execute(input);
        });

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "Product",
            It.IsAny<Guid?>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Send_Null_OldValues_When_Creating_Product()
    {
        object? oldValues = "NOT_NULL";

        _auditMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                AuditAction.CREATE,
                "Product",
                It.IsAny<Guid?>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ))
            .Callback<Guid?, string, AuditAction, string, Guid?, object, object>(
                (u, n, a, e, id, oldV, newV) =>
                {
                    oldValues = oldV;
                });

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new RegisterProductInput
            {
                Name = "Agua Test",
                Type = TypeProduct.Water.ToString(),
                Price = 10,
                Quantity = 5
            };

            await useCase.Execute(input);
        });

        oldValues.Should().BeNull();
    }

    private RegisterProduct CreateUseCase(AppDbContext db)
    {
        return new RegisterProduct(
            new ProductRepository(db),
            _auditMock.Object,
            _userMock.Object
        );
    }

    private async Task<Guid> SeedProduct(string name, bool isActive)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create(name).Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(isActive ? 5 : 0).Value!
            );

            if (!isActive)
                product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });
    }
}
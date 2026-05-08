using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using AquaGas.Api.Shared.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;

public class UpdateProductTests : BaseIntegrationTest
{
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<IUserContextService> _userMock = new();

    public UpdateProductTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("test-user"));
    }

    [Fact]
    public async Task Should_Update_Product_Successfully()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "Updated Product",
                Price = 99
            };

            var result = await useCase.Execute(input, productId);

            result.IsSuccess.Should().BeTrue();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var product = await db.Products.FirstAsync(x => x.Id == productId);

            product.Name.Value.Should().Be("Updated Product");
            product.Price.Value.Should().Be(99);
        });
    }

    [Fact]
    public async Task Should_Fail_When_Product_Not_Found()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "X",
                Price = 10
            };

            var result = await useCase.Execute(input, Guid.NewGuid());

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_Name_Already_Exists()
    {
        var (p1, p2) = await ExecuteDbContextAsync(async db =>
        {
            var a = CreateProduct("Produto A");
            var b = CreateProduct("Produto B");

            db.Products.AddRange(a, b);
            await db.SaveChangesAsync();

            return (a.Id, b.Id);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "Produto A"
            };

            var result = await useCase.Execute(input, p2);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_Changing_Type_With_Stock()
    {
        var productId = await SeedProduct(initialQty: 5);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Type = "Gas"
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Call_AuditLog_On_Update()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "Audit Product"
            };

            await useCase.Execute(input, productId);
        });

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "Product",
            productId,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Validation_Fails()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = ""
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_No_Data_Provided()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput();

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Send_Correct_Snapshot_To_Audit()
    {
        var productId = await SeedProduct();

        object? capturedOld = null;
        object? capturedNew = null;

        _auditMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                AuditAction.UPDATE,
                "Product",
                It.IsAny<Guid?>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ))
            .Callback<Guid?, string, AuditAction, string, Guid?, object, object>(
                (u, n, a, e, id, oldV, newV) =>
                {
                    capturedOld = oldV;
                    capturedNew = newV;
                });

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "New Name",
                Price = 50
            };

            await useCase.Execute(input, productId);
        });

        capturedOld.Should().NotBeNull();
        capturedNew.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Persist_Changes_In_Database()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateProductInput
            {
                Name = "Persisted Name"
            };

            await useCase.Execute(input, productId);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var product = await db.Products.FirstAsync(x => x.Id == productId);

            product.Name.Value.Should().Be("Persisted Name");
        });
    }

    private UpdateProduct CreateUseCase(AppDbContext db)
    {
        return new UpdateProduct(
            new ProductRepository(db),
            _auditMock.Object,
            _userMock.Object
        );
    }

    private async Task<Guid> SeedProduct(int initialQty = 0)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Original Product").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(initialQty).Value!
            );

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });
    }

    private static ProductEntity CreateProduct(string name)
    {
        return new Product(
            ProductName.Create(name).Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(0).Value!
        );
    }
}
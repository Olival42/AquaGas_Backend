using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.IntegrationTests.Common;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.IntegrationTests.Fixtures;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;

public class UpdateStockTests : BaseIntegrationTest
{
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<IUserContextService> _userMock = new();

    public UpdateStockTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("test-user"));
    }

    [Fact]
    public async Task Should_Increase_Stock_And_Create_Movement()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 10,
                StockMovementType = "Entry",
                Reason = "Entrada estoque"
            };

            var result = await useCase.Execute(input, productId);

            result.IsSuccess.Should().BeTrue();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var product = await db.Products.FirstAsync(x => x.Id == productId);
            var movement = await db.StockMovements.FirstOrDefaultAsync(x => x.ProductId == productId);

            product.Quantity.Value.Should().BeGreaterThan(0);
            movement.Should().NotBeNull();
            movement!.Quantity.Should().Be(10);
            movement.Type.Should().Be(StockMovementType.Entry);
        });
    }

    [Fact]
    public async Task Should_Decrease_Stock_And_Create_Movement()
    {
        var productId = await SeedProduct(initialQty: 20);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 5,
                StockMovementType = "Exit",
                Reason = "Saída estoque"
            };

            var result = await useCase.Execute(input, productId);

            result.IsSuccess.Should().BeTrue();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var product = await db.Products.FirstAsync(x => x.Id == productId);
            var movement = await db.StockMovements.FirstAsync(x => x.ProductId == productId);

            product.Quantity.Value.Should().Be(15);
            movement.Type.Should().Be(StockMovementType.Exit);
        });
    }

    [Fact]
    public async Task Should_Fail_When_Product_Not_Found()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 10,
                StockMovementType = "Entry",
                Reason = "teste"
            };

            var result = await useCase.Execute(input, Guid.NewGuid());

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Call_AuditLog()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 10,
                StockMovementType = "Entry",
                Reason = "audit test"
            };

            await useCase.Execute(input, productId);
        });

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.UPDATE,
            "Product",
            It.IsAny<Guid>(),
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

            var input = new UpdateStockInput
            {
                Quantity = 0,
                StockMovementType = "",
                Reason = ""
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Is_Invalid()
    {
        var productId = await SeedProduct();

        _userMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Validation("No user")));

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 5,
                StockMovementType = "Entry",
                Reason = "test"
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Is_Invalid()
    {
        var productId = await SeedProduct();

        _userMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(Error.Validation("No username")));

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 5,
                StockMovementType = "Entry",
                Reason = "test"
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Persist_StockMovement_In_Database()
    {
        var productId = await SeedProduct();

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 3,
                StockMovementType = "Entry",
                Reason = "movement test"
            };

            await useCase.Execute(input, productId);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var exists = await db.StockMovements.AnyAsync();

            exists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Should_Send_Correct_Snapshot_To_AuditLog()
    {
        var productId = await SeedProduct(initialQty: 10);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 5,
                StockMovementType = "Entry",
                Reason = "audit"
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
    public async Task Should_Fail_When_Exit_Exceeds_Stock()
    {
        var productId = await SeedProduct(initialQty: 5);

        await ExecuteDbContextAsync(async db =>
        {
            var useCase = CreateUseCase(db);

            var input = new UpdateStockInput
            {
                Quantity = 10,
                StockMovementType = "Exit",
                Reason = "over withdraw"
            };

            var result = await useCase.Execute(input, productId);

            result.IsFailure.Should().BeTrue();
        });
    }

    private UpdateStock CreateUseCase(AppDbContext db)
    {
        return new UpdateStock(
            new ProductRepository(db),
            _auditMock.Object,
            _userMock.Object,
            new StockMovementRepository(db)
        );
    }

    private async Task<Guid> SeedProduct(int initialQty = 10)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Test Product").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(initialQty).Value!
            );

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });
    }
}
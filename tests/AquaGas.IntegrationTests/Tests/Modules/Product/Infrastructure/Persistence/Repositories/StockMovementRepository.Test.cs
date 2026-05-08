using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class StockMovementRepositoryTests : BaseIntegrationTest
{
    public StockMovementRepositoryTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_ShouldPersistStockMovement()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Teste");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            var repository = new StockMovementRepository(db);

            var movement = StockMovement.Create(
                productId: productId,
                quantity: 10,
                type: StockMovementType.Entry,
                reason: "Entrada inicial",
                createdBy: userId
            );

            await repository.AddAsync(movement);
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.StockMovements
                .FirstOrDefaultAsync(x => x.ProductId == productId));

        saved.Should().NotBeNull();
        saved!.Quantity.Should().Be(10);
        saved.Reason.Should().Be("Entrada inicial");
        saved.Type.Should().Be(StockMovementType.Entry);
        saved.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task AddAsync_ShouldNotPersistWithoutSaveChanges()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Teste");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            var repository = new StockMovementRepository(db);

            var movement = StockMovement.Create(
                productId: productId,
                quantity: 5,
                type: StockMovementType.Exit,
                reason: "Saída teste",
                createdBy: userId
            );

            await repository.AddAsync(movement);

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.StockMovements
                .FirstOrDefaultAsync(x => x.ProductId == productId));

        saved.Should().BeNull();
    }

    private static Product CreateProduct(string name)
    {
        return new Product(
            ProductName.Create(name).Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(5).Value!
        );
    }
}
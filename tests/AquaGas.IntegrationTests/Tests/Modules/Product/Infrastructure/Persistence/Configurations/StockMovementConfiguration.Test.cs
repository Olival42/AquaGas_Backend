using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class StockMovementConfigurationTests : BaseIntegrationTest
{
    public StockMovementConfigurationTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
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

    [Fact]
    public async Task Should_Persist_StockMovement_With_Configuration()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Config");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            db.StockMovements.Add(StockMovement.Create(
                productId,
                StockMovementType.Entry,
                10,
                "Teste config",
                userId
            ));

            await db.SaveChangesAsync();
            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.StockMovements.FirstAsync());

        saved.Should().NotBeNull();
        saved.Type.Should().Be(StockMovementType.Entry);
        saved.Reason.Should().Be("Teste config");
    }

    [Fact]
    public async Task Should_Persist_Enum_As_String_In_Database()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Enum");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            db.StockMovements.Add(StockMovement.Create(
                productId,
                StockMovementType.Exit,
                5,
                "Enum test",
                userId
            ));

            await db.SaveChangesAsync();
            return Task.CompletedTask;
        });

        var raw = await ExecuteDbContextAsync(async db =>
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"Type\" FROM \"StockMovements\" LIMIT 1";

            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString();
        });

        raw.Should().Be("Exit");
    }

    [Fact]
    public async Task Should_Persist_CreatedBy_Even_When_InvalidGuid()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var invalidUserId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            db.StockMovements.Add(StockMovement.Create(
                productId,
                StockMovementType.Entry,
                10,
                "teste",
                invalidUserId
            ));

            await db.SaveChangesAsync();
            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.StockMovements.FirstAsync());

        saved.CreatedBy.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Should_Be_Linked_To_Product()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto FK");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            db.StockMovements.Add(StockMovement.Create(
                productId,
                StockMovementType.Entry,
                7,
                "FK test",
                userId
            ));

            await db.SaveChangesAsync();
            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.StockMovements
                .Include(x => x.Product)
                .FirstAsync());

        saved.Product.Should().NotBeNull();
        saved.Product!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task Should_Block_Product_Delete_When_Has_StockMovements()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Restrict");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            db.StockMovements.Add(StockMovement.Create(
                productId,
                StockMovementType.Entry,
                5,
                "teste restrict",
                userId
            ));

            await db.SaveChangesAsync();
            return Task.CompletedTask;
        });

        var act = async () =>
        {
            await ExecuteDbContextAsync(async db =>
            {
                var product = await db.Products.FirstAsync(x => x.Id == productId);

                db.Products.Remove(product);
                await db.SaveChangesAsync();

                return Task.CompletedTask;
            });
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
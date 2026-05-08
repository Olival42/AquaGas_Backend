using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ProductConfigurationTests : BaseIntegrationTest
{
    public ProductConfigurationTests(PostgreSqlContainerFixture fixture)
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
    public async Task Should_Persist_Product_With_Owned_Types()
    {
        var productId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Teste");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        saved.Should().NotBeNull();

        saved.Name.Value.Should().Be("Produto Teste");
        saved.Price.Value.Should().Be(10);
        saved.Quantity.Value.Should().Be(5);
        saved.Type.Should().Be(TypeProduct.Water);
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Persist_NormalizedName()
    {
        var productId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Agua Crystal");

            db.Products.Add(product);
            await SaveNormalizedName(db, product);

            productId = product.Id;

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        saved.NormalizedName.Should().Be("AGUA CRYSTAL");
    }

    private static async Task SaveNormalizedName(DbContext db, Product product)
    {
        product.GetType()
            .GetProperty("NormalizedName")!
            .SetValue(product, "AGUA CRYSTAL");

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_Enforce_Unique_Name()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(CreateProduct("Produto X"));
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var act = async () =>
        {
            await ExecuteDbContextAsync(async db =>
            {
                db.Products.Add(CreateProduct("Produto X"));
                await db.SaveChangesAsync();

                return Task.CompletedTask;
            });
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Enforce_Unique_NormalizedName()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var p1 = CreateProduct("Agua Crystal");

            db.Products.Add(p1);
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var act = async () =>
        {
            await ExecuteDbContextAsync(async db =>
            {
                var p2 = CreateProduct("AGUA CRYSTAL");

                db.Products.Add(p2);
                await db.SaveChangesAsync();

                return Task.CompletedTask;
            });
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Store_Type_As_String()
    {
        var productId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Enum");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;

            return Task.CompletedTask;
        });

        var raw = await ExecuteDbContextAsync(async db =>
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"Type\" FROM \"Products\" LIMIT 1";

            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString();
        });

        raw.Should().Be(TypeProduct.Water.ToString());
    }

    [Fact]
    public async Task Should_Persist_Price_With_DecimalPrecision()
    {
        var productId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create("Produto Price").Value!,
                TypeProduct.Water,
                Price.Create(99.99m).Value!,
                StockQuantity.Create(5).Value!
            );

            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        saved.Price.Value.Should().Be(99.99m);
    }

    [Fact]
    public async Task Should_Persist_IsActive_AsTrue_ByDefault()
    {
        var productId = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Active");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        saved.IsActive.Should().BeTrue();
    }
}
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Modules.Product.Infrastructure.Persistence.Repositories;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ProductRepositoryTests : BaseIntegrationTest
{
    public ProductRepositoryTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_ShouldPersistProduct()
    {
        var product = CreateProduct("Produto 1");

        await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);

            await repo.AddAsync(product);
            await repo.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var saved = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstOrDefaultAsync(x => x.NormalizedName == "PRODUTO 1"));

        saved.Should().NotBeNull();
        saved!.Name.Value.Should().Be("Produto 1");
    }

    [Fact]
    public async Task GetById_ShouldReturnProduct_WhenExists()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto X");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.GetByIdAsync(id);
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenInactiveAndOnlyActiveTrue()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Inativo", 0);
            product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.GetByIdAsync(id, onlyActive: true);
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithInactiveProductAndOnlyActiveTrue_ShouldReturnNull()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Inativo", 0);
            product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
            {
                var repo = new ProductRepository(db);
                return await repo.GetByIdAsync(productId, onlyActive: true);
            });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithInactiveProductAndOnlyActiveFalse_ShouldReturnProduct()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Inativo", 0);
            product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
            {
                var repo = new ProductRepository(db);
                return await repo.GetByIdAsync(productId, onlyActive: false);
            });

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetByName_ShouldReturnProduct_ByNormalizedName()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(CreateProduct("Agua Mineral"));
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.GetByNameAsync("agua mineral");
        });

        result.Should().NotBeNull();
        result!.Name.Value.Should().Be("Agua Mineral");
    }

    [Fact]
    public async Task GetByNameAsync_ShouldFindProductIgnoringCaseAndSpaces()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(CreateProduct("Agua Crystal"));
            await db.SaveChangesAsync();
        });

        var result = await ExecuteDbContextAsync(async db =>
             {
                 var repo = new ProductRepository(db);
                 return await repo.GetByNameAsync("  AGUA   crystal ");
             });

        result.Should().NotBeNull();
        result!.Name.Value.Should().Be("Agua Crystal");
    }

    [Fact]
    public async Task AnyByName_ShouldReturnTrue_WhenExists()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Products.Add(CreateProduct("Produto 1"));
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var exists = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.AnyByNameAsync("Produto 1");
        });

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyByName_ShouldReturnFalse_WhenInactiveAndOnlyActiveTrue()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var p = CreateProduct("Produto X", 0);
            p.Deactivate();

            db.Products.Add(p);
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var exists = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.AnyByNameAsync("Produto X", onlyActive: true);
        });

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByName_ShouldIgnoreGivenId()
    {
        Guid id1 = Guid.Empty;
        Guid id2 = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var p1 = CreateProduct("Produto Igual");
            var p2 = CreateProduct("Outro Produto");

            db.Products.AddRange(p1, p2);
            await db.SaveChangesAsync();

            id1 = p1.Id;
            id2 = p2.Id;

            return Task.CompletedTask;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.AnyByNameAsync("Produto Igual", id1);
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByNameAsync_ShouldIgnoreInactiveProducts()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto", 0);
            product.Deactivate();

            db.Products.Add(product);
            await db.SaveChangesAsync();
        });

        var result = await ExecuteDbContextAsync(async db =>
            {
                var repo = new ProductRepository(db);
                return await repo.AnyByNameAsync("Produto");
            });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByNameAsync_WithIgnoreId_ShouldExcludeSameProduct()
    {
        Guid id = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            id = product.Id;
        });

        var result = await ExecuteDbContextAsync(async db =>
            {
                var repo = new ProductRepository(db);
                return await repo.AnyByNameAsync("Produto", id);
            });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyActive_WhenOnlyActiveTrue()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var active = CreateProduct("Ativo");
            var inactive = CreateProduct("Inativo", 0);
            inactive.Deactivate();

            db.Products.AddRange(active, inactive);
            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var result = await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);
            return await repo.GetAllAsync();
        });

        result.Should().Contain(x => x.Name.Value == "Ativo");
        result.Should().NotContain(x => x.Name.Value == "Inativo");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveProducts()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var active = CreateProduct("Ativo");
            var inactive = CreateProduct("Inativo", 0);

            inactive.Deactivate();

            db.Products.AddRange(active, inactive);
            await db.SaveChangesAsync();
        });

        var result = await ExecuteDbContextAsync(async db =>
            {
                var repo = new ProductRepository(db);
                return await repo.GetAllAsync();
            });

        result.Should().ContainSingle(x => x.Name.Value == "Ativo");
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Original");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);

            var product = await db.Products.FirstAsync(x => x.Id == id);

            product.Update(
                ProductName.Create("Atualizado").Value!,
                TypeProduct.Water,
                Price.Create(99).Value!,
                StockQuantity.Create(10).Value!
            );

            repo.Update(product);
            await repo.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var updated = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == id));

        updated.Name.Value.Should().Be("Atualizado");
        updated.Price.Value.Should().Be(99);
    }

    [Fact]
    public async Task Update_ShouldMarkEntityAsModified()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        await ExecuteDbContextAsync(async db =>
        {
            var repo = new ProductRepository(db);

            var product = await db.Products.FirstAsync(x => x.Id == productId);

            product.Update(
                ProductName.Create("Novo Nome").Value!,
                TypeProduct.Water,
                Price.Create(50).Value!,
                StockQuantity.Create(10).Value!
            );

            repo.Update(product);

            await db.SaveChangesAsync();

            return Task.CompletedTask;
        });

        var updated = await ExecuteDbContextAsync(async db =>
            await db.Products.FirstAsync(x => x.Id == productId));

        updated.Name.Value.Should().Be("Novo Nome");
        updated.Price.Value.Should().Be(50);
    }

    private static Product CreateProduct(string name, int quantity = 5)
    {
        return new Product(
            ProductName.Create(name).Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }
}
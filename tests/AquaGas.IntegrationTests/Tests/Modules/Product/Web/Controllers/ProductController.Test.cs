using System.Net;
using System.Net.Http.Json;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ProductEndpointsTests : BaseIntegrationTest
{
    public ProductEndpointsTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task PostRegister_WithManagerToken_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var persisted = await ExecuteDbContextAsync(async db =>
            await db.Set<Product>()
                .FirstOrDefaultAsync(x => x.NormalizedName == "AGUA CRYSTAL"));

        persisted.Should().NotBeNull();
        persisted!.Name.Value.Should().Be("Agua Crystal");
    }

    [Fact]
    public async Task PostRegister_WithoutToken_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRegister_WithEmployeeRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient("Employee");

        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostRegister_WithInactiveExistingProduct_ShouldReactivate()
    {
        var name = $"Produto Reativado {Guid.NewGuid()}";

        await ExecuteDbContextAsync(async db =>
        {
            var product = new Product(
                ProductName.Create(name).Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                StockQuantity.Create(0).Value!
            );

            product.Deactivate();

            db.Add(product);
            await db.SaveChangesAsync();
        });

    var client = CreateAuthenticatedClient("Manager");

    var input = new RegisterProductInput
    {
        Name = name,
        Type = "Water",
        Price = 20,
        Quantity = 10
    };

    var response = await client.PostAsJsonAsync("/api/products/register", input);

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var product = await ExecuteDbContextAsync(async db =>
        await db.Set<Product>()
            .FirstAsync(x => x.Name.Value == name));
    Console.WriteLine(product.IsActive);

    product.IsActive.Should().BeTrue();
}

    [Fact]
    public async Task PostRegister_WithExistingActiveProduct_ShouldReturnConflict()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Add(CreateProduct("Produto"));
            await db.SaveChangesAsync();
        });

        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterProductInput
        {
            Name = "Produto",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync("/api/products/register", input);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_WithExistingProduct_ReturnsOk()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Teste");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveProducts()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var active = CreateProduct(
                "Ativo",
                5
            );

            var inactive = CreateProduct(
                "Inativo",
                0
            );

            inactive.Deactivate();

            db.Products.AddRange(active, inactive);

            await db.SaveChangesAsync();
        });

        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Ativo");
        content.Should().NotContain("Inativo");
    }

    [Fact]
    public async Task GetAll_WhenNoProducts_ShouldReturnEmptyList()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("[]");
    }

    [Fact]
    public async Task Delete_WithManagerRole_ReturnsNoContent()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var quantity = StockQuantity.Create(0).Value!;

            var product = new Product(
                ProductName.Create("Produto").Value!,
                TypeProduct.Water,
                Price.Create(10).Value!,
                quantity
            );

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync($"/api/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var product = await ExecuteDbContextAsync(async db =>
            await db.Set<Product>().FirstAsync(x => x.Id == productId));

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithEmployeeRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient("Employee");

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithStockGreaterThanZero_ShouldReturnConflict()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto", 5);

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync($"/api/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        var client = CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchProduct_WithValidPayload_UpdatesPersistedData()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto Original");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateProductInput
        {
            Name = "Produto Atualizado",
            Price = 99
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await ExecuteDbContextAsync(async db =>
            await db.Set<Product>().FirstAsync(x => x.Id == productId));

        product.Name.Value.Should().Be("Produto Atualizado");
        product.Price.Value.Should().Be(99);
    }

    [Fact]
    public async Task PatchProduct_WithStockGreaterThanZero_ShouldBlockTypeChange()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto", 5);

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var response = await client.PatchAsJsonAsync($"/api/products/{productId}", input);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PatchProduct_WithDuplicateName_ShouldReturnConflict()
    {
        Guid id1 = Guid.Empty;
        Guid id2 = Guid.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var p1 = CreateProduct("Produto 1");
            var p2 = CreateProduct("Produto 2");

            db.AddRange(p1, p2);
            await db.SaveChangesAsync();

            id1 = p1.Id;
            id2 = p2.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateProductInput
        {
            Name = "Produto 1"
        };

        var response = await client.PatchAsJsonAsync($"/api/products/{id2}", input);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PatchStock_Entry_ShouldIncreaseStock()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Entry",
            Reason = "Entrada"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/stock",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await ExecuteDbContextAsync(async db =>
            await db.Set<Product>().FirstAsync(x => x.Id == productId));

        product.Quantity.Value.Should().Be(15);
    }

    [Fact]
    public async Task PatchStock_Exit_ShouldDecreaseStock()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 3,
            StockMovementType = "Exit",
            Reason = "Saida"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/stock",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await ExecuteDbContextAsync(async db =>
            await db.Set<Product>().FirstAsync(x => x.Id == productId));

        product.Quantity.Value.Should().Be(2);
    }

    [Fact]
    public async Task PatchStock_WithInsufficientStock_ReturnsConflict()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 999,
            StockMovementType = "Exit",
            Reason = "Saida"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/stock",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PatchStock_WithInvalidMovementType_ShouldReturnBadRequest()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "INVALID",
            Reason = "Teste"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/stock",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchStock_WithInvalidReason_ShouldReturnBadRequest()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "ab"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/stock",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchStock_ShouldPersistStockMovement()
    {
        var productId = await ExecuteDbContextAsync(async db =>
        {
            var product = CreateProduct("Produto");

            db.Add(product);
            await db.SaveChangesAsync();

            return product.Id;
        });

        var client = CreateAuthenticatedClient();

        var input = new UpdateStockInput
        {
            Quantity = 3,
            StockMovementType = "Entry",
            Reason = "Entrada teste"
        };

        await client.PatchAsJsonAsync($"/api/products/{productId}/stock", input);

        var movement = await ExecuteDbContextAsync(async db =>
            await db.Set<StockMovement>()
                .FirstOrDefaultAsync(x => x.ProductId == productId));

        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(3);
        movement.Reason.Should().Be("Entrada teste");
    }

    private static Product CreateProduct(
        string name,
        int quantity = 5)
    {
        return new Product(
            ProductName.Create(name).Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }
}
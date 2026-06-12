using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Product;

[Collection("Integration")]
public class ProductInfrastructureTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductInfrastructureTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task StockMovement_Exit_ReducesQuantity()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Exit Infra",
            Type = "Gas",
            Price = 100.00m,
            Quantity = 50
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString()!;

        var exitContent = JsonContent.Create(new
        {
            StockMovementType = "Exit",
            Quantity = 10,
            Reason = "Saída teste infra"
        });
        var exitResponse = await client.PatchAsync($"/api/products/{productId}/stock", exitContent);
        exitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exitJson = await exitResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        exitJson.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(40);
    }

    [Fact]
    public async Task StockMovement_ExitExceedsStock_ReturnsError()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Overflow Infra",
            Type = "Water",
            Price = 10.00m,
            Quantity = 5
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString()!;

        var exitContent = JsonContent.Create(new
        {
            StockMovementType = "Exit",
            Quantity = 100,
            Reason = "Saída excedente infra"
        });
        var exitResponse = await client.PatchAsync($"/api/products/{productId}/stock", exitContent);

        exitResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MultipleStockMovements_TrackCumulativeQuantity()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Multi Move",
            Type = "Gas",
            Price = 60.00m,
            Quantity = 20
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString()!;

        await client.PatchAsync($"/api/products/{productId}/stock",
            JsonContent.Create(new { StockMovementType = "Entry", Quantity = 30, Reason = "Entrada 1" }));
        await client.PatchAsync($"/api/products/{productId}/stock",
            JsonContent.Create(new { StockMovementType = "Exit", Quantity = 10, Reason = "Saída 1" }));
        await client.PatchAsync($"/api/products/{productId}/stock",
            JsonContent.Create(new { StockMovementType = "Entry", Quantity = 5, Reason = "Entrada 2" }));

        var getResponse = await client.GetAsync($"/api/products/{productId}");
        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(45);
    }

    [Fact]
    public async Task DeactivatedProduct_NotReturnedInGetById()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Deact Infra",
            Type = "Gas",
            Price = 30.00m,
            Quantity = 10
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString()!;

        // First decrease stock to 0
        var decreaseContent = JsonContent.Create(new
        {
            StockMovementType = "Exit",
            Quantity = 10,
            Reason = "Decrease to zero for deactivation"
        });
        var decreaseResponse = await client.PatchAsync($"/api/products/{productId}/stock", decreaseContent);
        decreaseResponse.EnsureSuccessStatusCode();

        // Now deactivate
        await client.DeleteAsync($"/api/products/{productId}");

        // Check GetById after deactivation
        var getResponse = await client.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

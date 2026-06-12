using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Sale;

[Collection("Integration")]
public class SaleInfrastructureTests : IntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SaleInfrastructureTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    private async Task<string> CreateProductAsync(HttpClient client, string name, int qty = 50)
    {
        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = name,
            Type = "Gas",
            Price = 100.00m,
            Quantity = qty
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Sale_ReducesProductStock_Correctly()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Sale Stock Infra", 40);

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[] { new { ProductId = productId, Quantity = 15 } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var json = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(25);
    }

    [Fact]
    public async Task Sale_WithInsufficientStock_ReturnsError()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Sale NoStock Infra", 3);

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[] { new { ProductId = productId, Quantity = 100 } }
        });

        saleResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelSale_RestoresStock()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Sale Cancel Infra", 30);

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[] { new { ProductId = productId, Quantity = 10 } }
        });
        saleResponse.EnsureSuccessStatusCode();
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = saleJson.GetProperty("data").GetProperty("id").GetString()!;

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/sales/{saleId}/cancel",
            new { Reason = "Cancelamento infra" });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var json = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(30);
    }

    [Fact]
    public async Task CancelSale_AlreadyCanceled_ReturnsError()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Sale Double Cancel", 20);

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[] { new { ProductId = productId, Quantity = 5 } }
        });
        saleResponse.EnsureSuccessStatusCode();
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = saleJson.GetProperty("data").GetProperty("id").GetString()!;

        await client.PostAsJsonAsync($"/api/sales/{saleId}/cancel",
            new { Reason = "Primeiro cancelamento" });

        var secondCancel = await client.PostAsJsonAsync($"/api/sales/{saleId}/cancel",
            new { Reason = "Segundo cancelamento" });
        secondCancel.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sale_WithMultipleItems_CalculatesTotal()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var product1Response = await client.PostAsJsonAsync("/api/products/register", new
        { Name = "Multi Item 1", Type = "Gas", Price = 50.00m, Quantity = 100 });
        product1Response.EnsureSuccessStatusCode();
        var p1Json = await product1Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var product1Id = p1Json.GetProperty("data").GetProperty("id").GetString()!;

        var product2Response = await client.PostAsJsonAsync("/api/products/register", new
        { Name = "Multi Item 2", Type = "Water", Price = 20.00m, Quantity = 100 });
        product2Response.EnsureSuccessStatusCode();
        var p2Json = await product2Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var product2Id = p2Json.GetProperty("data").GetProperty("id").GetString()!;

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = product1Id, Quantity = 2 },
                new { ProductId = product2Id, Quantity = 3 }
            }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        saleJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(2);
        saleJson.GetProperty("data").GetProperty("total").GetDecimal().Should().Be(160.00m);
    }
}

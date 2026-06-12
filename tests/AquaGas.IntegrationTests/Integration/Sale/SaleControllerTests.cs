using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Sale;

[Collection("Integration")]
public class SaleControllerTests : IntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SaleControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    private async Task<string> CreateProductAsync(HttpClient client, string name, int quantity = 50)
    {
        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = name,
            Type = "Gas",
            Price = 89.90m,
            Quantity = quantity
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Produto Venda 01");

        var response = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = productId, Quantity = 2 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Register_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_GetById_ReturnsRegisteredSale()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Produto Venda GetById");

        var registerResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = json.GetProperty("data").GetProperty("id").GetString();

        var getResponse = await client.GetAsync($"/api/sales/{saleId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("id").GetString().Should().Be(saleId);
    }

    [Fact]
    public async Task Cancel_ExistingSale_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var productId = await CreateProductAsync(client, "Produto Venda Cancel");

        var registerResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = json.GetProperty("data").GetProperty("id").GetString();

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/sales/{saleId}/cancel",
            new { Reason = "Teste de cancelamento" });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelJson = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        cancelJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithNonExistentProduct_ReturnsError()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[]
            {
                new { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            }
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict);
    }
}

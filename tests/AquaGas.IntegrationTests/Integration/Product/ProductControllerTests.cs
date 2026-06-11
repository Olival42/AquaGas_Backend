using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Product;

[Collection("Integration")]
public class ProductControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_AsManager_ReturnsCreated()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Botijão P13 Teste",
            Type = "Gas",
            Price = 89.90m,
            Quantity = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("name").GetString().Should().Be("Botijão P13 Teste");
        json.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task Register_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Sem Auth",
            Type = "Water",
            Price = 10.00m,
            Quantity = 10
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_GetById_ReturnsRegisteredProduct()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Galão 20L GetById",
            Type = "Water",
            Price = 15.00m,
            Quantity = 100
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString();

        var getResponse = await client.GetAsync($"/api/products/{productId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Galão 20L GetById");
    }

    [Fact]
    public async Task UpdateStock_Entry_IncreasesQuantity()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Estoque",
            Type = "Gas",
            Price = 50.00m,
            Quantity = 10
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString();

        var stockContent = JsonContent.Create(new
        {
            StockMovementType = "Entry",
            Quantity = 20,
            Reason = "Reposição de estoque"
        });
        var stockResponse = await client.PatchAsync($"/api/products/{productId}/stock", stockContent);

        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stockJson = await stockResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        stockJson.GetProperty("data").GetProperty("quantity").GetInt32().Should().Be(30);
    }

    [Fact]
    public async Task Deactivate_AsManager_ReturnsNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Deletar",
            Type = "Water",
            Price = 5.00m,
            Quantity = 5
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/products/{productId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Update",
            Type = "Gas",
            Price = 70.00m,
            Quantity = 25
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = json.GetProperty("data").GetProperty("id").GetString();

        var patchContent = JsonContent.Create(new { Name = "Produto Atualizado", Price = 80.00m });
        var updateResponse = await client.PatchAsync($"/api/products/{productId}", patchContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updateJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Produto Atualizado");
    }
}

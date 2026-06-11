using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.E2E.Customer;

[Collection("Integration")]
public class CustomerFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CustomerFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullCustomerLifecycle_Register_Get_Update_Deactivate()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente Lifecycle E2E",
            Document = "15935748020",
            Email = "lifecycle.e2e@email.com",
            Phone = "11999995001",
            Address = new
            {
                Street = "Rua Lifecycle",
                Neighborhood = "Jardim",
                Number = "500",
                City = "São Paulo",
                Cep = "05005000"
            }
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = registerJson.GetProperty("data").GetProperty("id").GetString()!;
        registerJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Cliente Lifecycle E2E");

        var getResponse = await client.GetAsync($"/api/customers/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("email").GetString().Should().Be("lifecycle.e2e@email.com");

        var patchContent = JsonContent.Create(new
        {
            Name = "Cliente Atualizado E2E",
            Phone = "11999995099"
        });
        var updateResponse = await client.PatchAsync($"/api/customers/{customerId}", patchContent);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updateJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Cliente Atualizado E2E");

        var getAllResponse = await client.GetAsync("/api/customers");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getAllJson = await getAllResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getAllJson.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDeleteResponse = await client.GetAsync($"/api/customers/{customerId}");
        afterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerWithSaleHistory_RegisterCustomer_MakeSale_CheckHistory()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var customerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente Historico E2E",
            Document = "26384715044",
            Email = "historico.e2e@email.com",
            Phone = "11999995002",
            Address = new
            {
                Street = "Rua Historico",
                Neighborhood = "Centro",
                Number = "100",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerJson = await customerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = customerJson.GetProperty("data").GetProperty("id").GetString()!;

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Historico E2E",
            Type = "Gas",
            Price = 50.00m,
            Quantity = 100
        });
        productResponse.EnsureSuccessStatusCode();
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            CustomerId = customerId,
            SaleItems = new[] { new { ProductId = productId, Quantity = 3 } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var historyResponse = await client.PostAsJsonAsync(
            $"/api/customers/{customerId}/consumption-history",
            new
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddDays(1)
            });
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyJson = await historyResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        historyJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}

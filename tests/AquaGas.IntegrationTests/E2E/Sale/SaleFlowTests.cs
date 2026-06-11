using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.E2E.Sale;

[Collection("Integration")]
public class SaleFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SaleFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSaleFlow_RegisterProduct_RegisterCustomer_Sell_Verify_Cancel()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Botijão E2E Sale",
            Type = "Gas",
            Price = 100.00m,
            Quantity = 30
        });
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;
        var initialStock = productJson.GetProperty("data").GetProperty("quantity").GetInt32();

        var customerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente E2E Sale",
            Document = "93748261003",
            Email = "e2e.sale@email.com",
            Phone = "11999991000",
            Address = new
            {
                Street = "Rua E2E",
                Neighborhood = "Centro",
                Number = "1",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var customerJson = await customerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = customerJson.GetProperty("data").GetProperty("id").GetString()!;

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            CustomerId = customerId,
            SaleItems = new[] { new { ProductId = productId, Quantity = 5 } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = saleJson.GetProperty("data").GetProperty("id").GetString()!;

        var getResponse = await client.GetAsync($"/api/sales/{saleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var productAfterSale = await client.GetAsync($"/api/products/{productId}");
        var productAfterJson = await productAfterSale.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        productAfterJson.GetProperty("data").GetProperty("quantity").GetInt32()
            .Should().Be(initialStock - 5);

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/sales/{saleId}/cancel", new { Reason = "Cancelamento E2E" });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var productAfterCancel = await client.GetAsync($"/api/products/{productId}");
        var productCancelJson = await productAfterCancel.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        productCancelJson.GetProperty("data").GetProperty("quantity").GetInt32()
            .Should().Be(initialStock);
    }

    [Fact]
    public async Task SaleWithDiscount_AppliesDiscountCorrectly()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Desconto E2E",
            Type = "Water",
            Price = 100.00m,
            Quantity = 20
        });
        productResponse.EnsureSuccessStatusCode();

        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            Discount = 10.0,
            SaleItems = new[] { new { ProductId = productId, Quantity = 2 } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        saleJson.GetProperty("data").GetProperty("discount").GetDouble().Should().Be(10.0);
        saleJson.GetProperty("data").GetProperty("total").GetDecimal().Should().BeLessThan(200.00m);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.E2E.Report;

[Collection("Integration")]
public class ReportFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SalesReport_AfterCreatingSale_ReflectsInReport()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto Report E2E",
            Type = "Gas",
            Price = 120.00m,
            Quantity = 50
        });
        productResponse.EnsureSuccessStatusCode();
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var saleResponse = await client.PostAsJsonAsync("/api/sales/register", new
        {
            SaleItems = new[] { new { ProductId = productId, Quantity = 3 } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var start = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss");
        var end = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss");

        var reportResponse = await client.GetAsync($"/api/reports/sales?Start={start}&End={end}");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportJson = await reportResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        reportJson.GetProperty("success").GetBoolean().Should().BeTrue();
        reportJson.GetProperty("data").GetProperty("summary").GetProperty("totalSales").GetInt32()
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StockMovementReport_AfterStockEntry_ReflectsInReport()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var productResponse = await client.PostAsJsonAsync("/api/products/register", new
        {
            Name = "Produto StockReport E2E",
            Type = "Water",
            Price = 15.00m,
            Quantity = 10
        });
        productResponse.EnsureSuccessStatusCode();
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var productId = productJson.GetProperty("data").GetProperty("id").GetString()!;

        var stockContent = JsonContent.Create(new
        {
            StockMovementType = "Entry",
            Quantity = 25,
            Reason = "Reposição Report E2E"
        });
        var stockResponse = await client.PatchAsync($"/api/products/{productId}/stock", stockContent);
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var start = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss");
        var end = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss");

        var reportResponse = await client.GetAsync(
            $"/api/reports/stock-movements?Start={start}&End={end}");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportJson = await reportResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        reportJson.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ContractPenaltyReport_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var reportResponse = await client.GetAsync("/api/reports/contract-penalties");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportJson = await reportResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        reportJson.GetProperty("success").GetBoolean().Should().BeTrue();
        reportJson.GetProperty("data").GetProperty("summary").ValueKind.Should().Be(JsonValueKind.Object);
    }
}

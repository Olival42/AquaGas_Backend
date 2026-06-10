using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Report;

[Collection("Integration")]
public class ReportControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSalesReport_AsManager_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var start = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync(
            $"/api/reports/sales?Start={start}&End={end}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetStockMovementReport_AsManager_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var start = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync(
            $"/api/reports/stock-movements?Start={start}&End={end}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetContractPenaltyReport_AsManager_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/reports/contract-penalties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetSalesReport_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var start = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync(
            $"/api/reports/sales?Start={start}&End={end}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

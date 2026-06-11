using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Shared;

[Collection("Integration")]
public class MiddlewareTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenBlacklistMiddleware_BlocksRevokedToken()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accessToken = loginJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var beforeLogout = await client.GetAsync("/api/products");
        beforeLogout.StatusCode.Should().Be(HttpStatusCode.OK);

        await client.PostAsync("/api/auth/logout", null);

        var afterLogout = await client.GetAsync("/api/employees");
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_ReturnsJsonErrorEnvelope()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/nonexistent-endpoint-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResponseEnvelope_SuccessFormat_ContainsExpectedFields()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("success", out _).Should().BeTrue();
        json.TryGetProperty("data", out _).Should().BeTrue();
        json.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ResponseEnvelope_ErrorFormat_ContainsExpectedFields()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.TryGetProperty("error", out _).Should().BeTrue();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AquaGas.IntegrationTests.E2E.Auth;

[Collection("Integration")]
public class AuthFlowTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task FullAuthCycle_Login_UseToken_Refresh_Logout()
    {
        var client = Factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accessToken = loginJson.GetProperty("data").GetProperty("accessToken").GetString()!;
        accessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var protectedResponse = await client.GetAsync("/api/employees");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var newAccessToken = refreshJson.GetProperty("data").GetProperty("accessToken").GetString()!;
        newAccessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);

        var protectedResponse2 = await client.GetAsync("/api/products");
        protectedResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

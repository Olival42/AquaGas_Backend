using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AquaGas.IntegrationTests.Integration.Auth;

[Collection("Integration")]
public class AuthControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("data").GetProperty("user").GetProperty("userName").GetString()
            .Should().Be("gerente123");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "WrongPassword@1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "usernotexist",
            Password = "Admin@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WhenAuthenticated_ReturnsNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsNewToken()
    {
        var client = Factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

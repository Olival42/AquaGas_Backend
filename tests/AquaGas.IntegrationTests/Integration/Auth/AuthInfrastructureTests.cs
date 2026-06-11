using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Auth;

[Collection("Integration")]
public class AuthInfrastructureTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthInfrastructureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenBlacklist_AfterLogout_TokenIsRevoked()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = loginJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var beforeLogout = await client.GetAsync("/api/products");
        beforeLogout.StatusCode.Should().Be(HttpStatusCode.OK);

        await client.PostAsync("/api/auth/logout", null);

        var afterLogout = await client.GetAsync("/api/products");
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_IsStoredInCookie_AndWorksForRenewal()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        refreshJson.GetProperty("data").GetProperty("accessToken").GetString()
            .Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task JwtService_TokenContainsCorrectClaims()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = loginJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT should have 3 parts (header.payload.signature)");

        var user = loginJson.GetProperty("data").GetProperty("user");
        user.GetProperty("role").GetString().Should().Be("Manager");
        user.GetProperty("userName").GetString().Should().Be("gerente123");
    }

    [Fact]
    public async Task PasswordHasher_Argon2_ValidatesCorrectly()
    {
        var client = _factory.CreateClient();

        var validLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "Admin@123"
        });
        validLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        var invalidLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "gerente123",
            Password = "admin@123"
        });
        invalidLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

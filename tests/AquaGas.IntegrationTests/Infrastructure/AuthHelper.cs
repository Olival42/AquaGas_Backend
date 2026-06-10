using System.Net.Http.Json;
using System.Text.Json;

namespace AquaGas.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    private const string SeedUserName = "gerente123";
    private const string SeedPassword = "Admin@123";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = SeedUserName,
            Password = SeedPassword
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    public static void SetAuthHeader(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        SetAuthHeader(client, token);
        return client;
    }
}

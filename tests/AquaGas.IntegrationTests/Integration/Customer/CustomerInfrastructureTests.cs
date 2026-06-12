using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Customer;

[Collection("Integration")]
public class CustomerInfrastructureTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CustomerInfrastructureTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DuplicateDocument_ReturnsConflict()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var cpf = "52998225373";

        await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Primeiro CPF",
            Document = cpf,
            Email = "infra.first@email.com",
            Phone = "11999996001",
            Address = new
            {
                Street = "Rua A",
                Neighborhood = "Centro",
                Number = "1",
                City = "São Paulo",
                Cep = "01001000"
            }
        });

        var duplicateResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Segundo CPF",
            Document = cpf,
            Email = "infra.second@email.com",
            Phone = "11999996002",
            Address = new
            {
                Street = "Rua B",
                Neighborhood = "Centro",
                Number = "2",
                City = "São Paulo",
                Cep = "01001000"
            }
        });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivatedCustomer_NotReturnedInGetById()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Desativado Infra",
            Document = "52998225454",
            Email = "infra.deactivated@email.com",
            Phone = "11999996003",
            Address = new
            {
                Street = "Rua Deact",
                Neighborhood = "Centro",
                Number = "3",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = json.GetProperty("data").GetProperty("id").GetString()!;

        await client.DeleteAsync($"/api/customers/{id}");

        var getResponse = await client.GetAsync($"/api/customers/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAddress_PersistsCorrectly()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Atualizar Endereco",
            Document = "52998225535",
            Email = "infra.address@email.com",
            Phone = "11999996004",
            Address = new
            {
                Street = "Rua Original",
                Neighborhood = "Centro",
                Number = "100",
                City = "São Paulo",
                Cep = "01001000"
            }
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = json.GetProperty("data").GetProperty("id").GetString()!;
        var addressId = json.GetProperty("data").GetProperty("address")
            .GetProperty("id").GetString()!;

        var patchContent = JsonContent.Create(new
        {
            Address = new
            {
                AddressId = addressId,
                Street = "Rua Atualizada",
                City = "Rio de Janeiro"
            }
        });
        var updateResponse = await client.PatchAsync($"/api/customers/{id}", patchContent);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/customers/{id}");
        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("address")
            .GetProperty("street").GetString().Should().Be("Rua Atualizada");
        getJson.GetProperty("data").GetProperty("address")
            .GetProperty("city").GetString().Should().Be("Rio de Janeiro");
    }
}

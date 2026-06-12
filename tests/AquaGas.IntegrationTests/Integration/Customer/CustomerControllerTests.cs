using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Customer;

[Collection("Integration")]
public class CustomerControllerTests : IntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CustomerControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var cpf = "52998224725";

        var response = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Cliente Teste",
            Document = cpf,
            Email = "cliente.teste@email.com",
            Phone = "11999990001",
            Address = new
            {
                Street = "Rua Teste",
                Neighborhood = "Centro",
                Number = "100",
                City = "São Paulo",
                Cep = "01001000"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("name").GetString().Should().Be("Cliente Teste");
    }

    [Fact]
    public async Task Register_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Sem Auth",
            Document = "52998224806",
            Email = "noauth@email.com",
            Phone = "11999990099",
            Address = new
            {
                Street = "Rua X",
                Neighborhood = "Bairro",
                Number = "1",
                City = "SP",
                Cep = "01001000"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_GetById_ReturnsRegisteredCustomer()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var cpf = "52998224997";

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Busca Por ID",
            Document = cpf,
            Email = "busca.id@email.com",
            Phone = "11999990002",
            Address = new
            {
                Street = "Rua Busca",
                Neighborhood = "Centro",
                Number = "200",
                City = "São Paulo",
                Cep = "02002000"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = registerJson.GetProperty("data").GetProperty("id").GetString();

        var getResponse = await client.GetAsync($"/api/customers/{customerId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Busca Por ID");
    }

    [Fact]
    public async Task Deactivate_AsManager_ReturnsNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var cpf = "05244777017";

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Deletar Cliente",
            Document = cpf,
            Email = "deletar@email.com",
            Phone = "11999990003",
            Address = new
            {
                Street = "Rua Del",
                Neighborhood = "Centro",
                Number = "300",
                City = "São Paulo",
                Cep = "03003000"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = json.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        var cpf = "52998225101";

        var registerResponse = await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Atualizar Cliente",
            Document = cpf,
            Email = "atualizar@email.com",
            Phone = "11999990004",
            Address = new
            {
                Street = "Rua Upd",
                Neighborhood = "Centro",
                Number = "400",
                City = "São Paulo",
                Cep = "04004000"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = json.GetProperty("data").GetProperty("id").GetString();

        var patchContent = JsonContent.Create(new { Name = "Nome Atualizado" });
        var updateResponse = await client.PatchAsync($"/api/customers/{customerId}", patchContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updateJson.GetProperty("data").GetProperty("name").GetString().Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task ExistsByDocument_WithExistingDocument_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);
        await client.PostAsJsonAsync("/api/customers/register", new
        {
            Name = "Existe Doc",
            Document = "98765432100",
            Email = "existedoc@email.com",
            Phone = "11999990005",
            Address = new
            {
                Street = "Rua Doc",
                Neighborhood = "Centro",
                Number = "500",
                City = "São Paulo",
                Cep = "05005000"
            }
        });

        var response = await client.GetAsync("/api/customers/exists?document=98765432100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

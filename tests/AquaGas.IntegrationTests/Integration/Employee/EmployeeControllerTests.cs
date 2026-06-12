using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Employee;

[Collection("Integration")]
public class EmployeeControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_AsManager_ReturnsCreated()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new
            {
                UserName = "emptest01",
                Password = "Senha@123",
                Role = "Employee"
            },
            Employee = new
            {
                Name = "Funcionário Teste",
                Cpf = "52998224997",
                Email = "func.teste@empresa.com",
                Phone = "11988880002"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("employee").GetProperty("name").GetString()
            .Should().Be("Funcionário Teste");
    }

    [Fact]
    public async Task Register_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "noauth01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Sem Auth",
                Cpf = "52998224806",
                Email = "noauth@empresa.com",
                Phone = "11988880099"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync($"/api/employees/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var response = await client.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Register_GetById_ReturnsRegisteredEmployee()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "empget01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Busca Funcionário",
                Cpf = "52998224997",
                Email = "busca.func@empresa.com",
                Phone = "11988880002"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var employeeId = json.GetProperty("data").GetProperty("employee").GetProperty("id").GetString();

        var getResponse = await client.GetAsync($"/api/employees/{employeeId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getJson.GetProperty("data").GetProperty("employee").GetProperty("name").GetString()
            .Should().Be("Busca Funcionário");
    }

    [Fact]
    public async Task Deactivate_AsManager_ReturnsNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "empdel01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Deletar Func",
                Cpf = "52998225020",
                Email = "del.func@empresa.com",
                Phone = "11988880003"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var employeeId = json.GetProperty("data").GetProperty("employee").GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/employees/{employeeId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_AsManager_ReturnsOk()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "empupd01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Update Func",
                Cpf = "79522375004",
                Email = "upd.func@empresa.com",
                Phone = "11988880004"
            }
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var employeeId = json.GetProperty("data").GetProperty("employee").GetProperty("id").GetString();

        var patchContent = JsonContent.Create(new { Name = "Nome Atualizado Func" });
        var updateResponse = await client.PatchAsync($"/api/employees/{employeeId}", patchContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updateJson.GetProperty("data").GetProperty("employee").GetProperty("name").GetString()
            .Should().Be("Nome Atualizado Func");
    }
}

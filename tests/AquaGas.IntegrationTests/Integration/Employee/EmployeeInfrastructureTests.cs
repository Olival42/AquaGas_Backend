using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.Integration.Employee;

[Collection("Integration")]
public class EmployeeInfrastructureTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeInfrastructureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DuplicateCpf_ReturnsConflict()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infracpf01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Primeiro CPF Emp",
                Cpf = "14725836900",
                Email = "infra.emp.first@empresa.com",
                Phone = "11988886001"
            }
        });

        var duplicateResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infracpf02", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Segundo CPF Emp",
                Cpf = "14725836900",
                Email = "infra.emp.second@empresa.com",
                Phone = "11988886002"
            }
        });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DuplicateUserName_ReturnsConflict()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infrausr01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Primeiro User",
                Cpf = "95173684021",
                Email = "infra.usr.first@empresa.com",
                Phone = "11988886003"
            }
        });

        var duplicateResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infrausr01", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Segundo User",
                Cpf = "36925814752",
                Email = "infra.usr.second@empresa.com",
                Phone = "11988886004"
            }
        });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivatedEmployee_NotReturnedInGetById()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infradeac", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Deactivated Emp",
                Cpf = "74185296300",
                Email = "infra.deac@empresa.com",
                Phone = "11988886005"
            }
        });
        registerResponse.EnsureSuccessStatusCode();
        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = json.GetProperty("data").GetProperty("employee").GetProperty("id").GetString()!;

        await client.DeleteAsync($"/api/employees/{id}");

        var getResponse = await client.GetAsync($"/api/employees/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegisterEmployee_CreatesUserAndEmployee_Linked()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var registerResponse = await client.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "infralink", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Linked Emp",
                Cpf = "85296374100",
                Email = "infra.linked@empresa.com",
                Phone = "11988886006"
            }
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var userId = json.GetProperty("data").GetProperty("user").GetProperty("userId").GetString();
        var empId = json.GetProperty("data").GetProperty("employee").GetProperty("id").GetString();

        userId.Should().NotBeNullOrEmpty();
        empId.Should().NotBeNullOrEmpty();
        json.GetProperty("data").GetProperty("user").GetProperty("userName").GetString()
            .Should().Be("infralink");
    }
}

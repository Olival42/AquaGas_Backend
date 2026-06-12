using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AquaGas.IntegrationTests.Infrastructure;

using FluentAssertions;

namespace AquaGas.IntegrationTests.E2E.Employee;

[Collection("Integration")]
public class EmployeeFlowTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task FullEmployeeLifecycle_Register_Login_Operate_Update_Deactivate()
    {
        var managerClient = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        var registerResponse = await managerClient.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "e2eemp001", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Funcionário E2E Lifecycle",
                Cpf = "52998224997",
                Email = "e2e.emp.lifecycle@empresa.com",
                Phone = "11988885002"
            }
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var employeeId = registerJson.GetProperty("data").GetProperty("employee")
            .GetProperty("id").GetString()!;

        var empClient = Factory.CreateClient();
        var loginResponse = await empClient.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "e2eemp001",
            Password = "Senha@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var empToken = loginJson.GetProperty("data").GetProperty("accessToken").GetString()!;
        AuthHelper.SetAuthHeader(empClient, empToken);

        var productsResponse = await empClient.GetAsync("/api/products");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var customersResponse = await empClient.GetAsync("/api/customers");
        customersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var patchContent = JsonContent.Create(new { Name = "Funcionario Atualizado E2E" });
        var updateResponse = await managerClient.PatchAsync($"/api/employees/{employeeId}", patchContent);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateJson = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updateJson.GetProperty("data").GetProperty("employee")
            .GetProperty("name").GetString().Should().Be("Funcionario Atualizado E2E");

        var deactivateResponse = await managerClient.DeleteAsync($"/api/employees/{employeeId}");
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task EmployeeRole_CannotRegisterEmployee()
    {
        var managerClient = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        await managerClient.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "e2eemp002", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Employee Sem Permissao",
                Cpf = "98765432100",
                Email = "e2e.noperm@empresa.com",
                Phone = "11988885002"
            }
        });

        var empClient = Factory.CreateClient();
        var loginResponse = await empClient.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "e2eemp002",
            Password = "Senha@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        AuthHelper.SetAuthHeader(empClient, loginJson.GetProperty("data").GetProperty("accessToken").GetString()!);

        var registerAttempt = await empClient.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "shouldfail", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Nao Deveria",
                Cpf = "52998225454",
                Email = "fail@empresa.com",
                Phone = "11988885099"
            }
        });

        registerAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EmployeeRole_CannotAccessReports()
    {
        var managerClient = await AuthHelper.CreateAuthenticatedClientAsync(Factory);

        await managerClient.PostAsJsonAsync("/api/employees/register", new
        {
            User = new { UserName = "e2eemp003", Password = "Senha@123", Role = "Employee" },
            Employee = new
            {
                Name = "Employee Report Block",
                Cpf = "52998225535",
                Email = "e2e.noreport@empresa.com",
                Phone = "11988885003"
            }
        });

        var empClient = Factory.CreateClient();
        var loginResponse = await empClient.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "e2eemp003",
            Password = "Senha@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        AuthHelper.SetAuthHeader(empClient, loginJson.GetProperty("data").GetProperty("accessToken").GetString()!);

        var start = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var reportResponse = await empClient.GetAsync($"/api/reports/sales?Start={start}&End={end}");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

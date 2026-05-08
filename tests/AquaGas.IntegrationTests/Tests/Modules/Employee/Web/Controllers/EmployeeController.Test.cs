using System.Net;
using System.Net.Http.Json;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Auth.Domain.Models;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class EmployeeControllerTests : BaseIntegrationTest
{
    public EmployeeControllerTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Register_Should_Create_Employee()
    {
        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "João Silva",
                Cpf = "52998224725",
                Email = "joao@test.com",
                Phone = "44999999999"
            },
            User = new UserInput
            {
                UserName = "joaosilva",
                Password = "Joao@123",
                Role = "Manager"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.CPF.Value == "52998224725");

            employee.Should().NotBeNull();
            employee!.User.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_When_Cpf_Already_Exists()
    {
        await SeedEmployee();

        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Outro",
                Cpf = "52998224725",
                Email = "other@test.com",
                Phone = "44999999999"
            },
            User = new UserInput
            {
                UserName = "joaosilva",
                Password = "Joao@123",
                Role = "Manager"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Input_Is_Invalid()
    {
        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "",
                Cpf = "",
                Email = "invalid-email",
                Phone = ""
            },
            User = new UserInput
            {
                UserName = "",
                Password = "",
                Role = ""
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_When_UserName_Already_Exists()
    {
        await SeedEmployee();

        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Maria",
                Cpf = "52998224725",
                Email = "maria@test.com",
                Phone = "44999999998"
            },
            User = new UserInput
            {
                UserName = "joaosilva",
                Password = "Joao@123",
                Role = "Manager"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_Should_Reactivate_Employee_When_Inactive()
    {
        var employeeId = await SeedEmployee(isActive: false);

        var client = CreateAuthenticatedClient("Manager");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "João Novo",
                Cpf = "52998224725",
                Email = "novo@test.com",
                Phone = "44999999999"
            },
            User = new UserInput
            {
                UserName = "joaosilva",
                Password = "Joao@123",
                Role = "Manager"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees
                .Include(x => x.User)
                .FirstAsync(x => x.Id == employeeId);

            employee.IsActive.Should().BeTrue();
            employee.User!.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Register_Should_Require_Manager_Role()
    {
        var client = CreateAuthenticatedClient("Employee");

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Teste",
                Cpf = "52998224725",
                Email = "teste@test.com",
                Phone = "44999999999"
            },
            User = new UserInput
            {
                UserName = "joaosilva",
                Password = "Joao@123",
                Role = "Manager"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/employees/register",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Should_Return_Employee()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            $"/api/employees/{employeeId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            $"/api/employees/{Guid.NewGuid()}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Should_Return_Employees()
    {
        await SeedEmployee();

        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Joao Silva");
        content.Should().Contain("joaosilva");
    }

    [Fact]
    public async Task Update_Should_Update_Employee()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient("Manager");

        var input = new UpdateEmployeeInput
        {
            Name = "João Atualizado",
            Email = "updated@test.com"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{employeeId}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees
                .FirstAsync(x => x.Id == employeeId);

            employee.Name.Value.Should().Be("João Atualizado");
            employee.Email.Value.Should().Be("updated@test.com");
        });
    }

    [Fact]
    public async Task Update_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedClient("Manager");

        var input = new UpdateEmployeeInput
        {
            Name = "Teste"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{Guid.NewGuid()}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Should_Return_Conflict_When_Email_Already_Exists()
    {
        var employee1 = await SeedEmployee();

        await SeedEmployee(
            cpf: "78369141005",
            email: "other@test.com",
            username: "other"
        );

        var client = CreateAuthenticatedClient("Manager");

        var input = new UpdateEmployeeInput
        {
            Email = "other@test.com"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{employee1}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Should_Return_Conflict_When_UserName_Already_Exists()
    {
        var employee1 = await SeedEmployee();

        await SeedEmployee(
            cpf: "78369141005",
            username: "existinguser",
            email: "other@test.com"
        );

        var client = CreateAuthenticatedClient("Manager");

        var input = new UpdateEmployeeInput
        {
            UserName = "existinguser"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{employee1}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Should_Require_Manager_Role()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient("Employee");

        var input = new UpdateEmployeeInput
        {
            Name = "Teste"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{employeeId}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Should_Return_BadRequest_When_Input_Is_Invalid()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient("Manager");

        var input = new UpdateEmployeeInput
        {
            Email = "email-invalido"
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/employees/{employeeId}",
            input
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactive_Should_Deactivate_Employee()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync(
            $"/api/employees/{employeeId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees
                .FirstAsync(x => x.Id == employeeId);

            employee.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Deactive_Should_Return_NotFound()
    {
        var client = CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync(
            $"/api/employees/{Guid.NewGuid()}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactive_Should_Return_Conflict_When_User_Tries_To_Deactivate_Himself()
    {
        var (userId, _) = await SeedUserAsync();

        var client = CreateAuthenticatedClient(
            role: "Manager",
            userId: userId);

        var employeeId = await ExecuteDbContextAsync(async db =>
        {
            var user = await db.Users
                .FirstAsync(x => x.Id == userId);

            return user.EmployeeId;
        });

        var response = await client.DeleteAsync(
            $"/api/employees/{employeeId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoints_Should_Require_Authentication()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactive_Should_Require_Manager_Role()
    {
        var employeeId = await SeedEmployee();

        var client = CreateAuthenticatedClient("Employee");

        var response = await client.DeleteAsync(
            $"/api/employees/{employeeId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> SeedEmployee(
    string name = "Joao Silva",
    string cpf = "52998224725",
    string email = "joao@aquagas.com",
    string phone = "44999999999",
    string username = "joaosilva",
    Role role = Role.Employee,
    bool isActive = true)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var employee = new EmployeeEntity(
                EmployeeName.Create(name).Value!,
                Cpf.Create(cpf).Value!,
                Email.Create(email).Value!,
                Phone.Create(phone).Value!
            );

            var user = new User(
                UserName.Create(username).Value!,
                "HASH",
                role,
                employee.Id
            );

            employee.AssignUser(user);

            if (!isActive)
                employee.Deactive();

            db.Employees.Add(employee);

            await db.SaveChangesAsync();

            return employee.Id;
        });
    }
}
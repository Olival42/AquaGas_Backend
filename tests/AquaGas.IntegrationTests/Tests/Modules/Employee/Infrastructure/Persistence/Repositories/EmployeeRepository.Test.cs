using AquaGas.API.Modules.Employee.Infrastructure.Persistence.Repositories;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.IntegrationTests.Collections;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using AquaGas.Api.Shared.Infrastructure.Persistence;

[Collection(IntegrationTestCollection.Name)]
public class EmployeeRepositoryTests : BaseIntegrationTest
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EmployeeRepositoryTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    private EmployeeRepository CreateRepository(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        return new EmployeeRepository(dbContext);
    }

    private static EmployeeEntity CreateEmployee(
    string? name = null,
    string? cpf = null,
    string? email = null,
    string? phone = null)
    {
        return new EmployeeEntity(
            EmployeeName.Create(name ?? $"Employee {Guid.NewGuid()}").Value!,
            Cpf.Create(cpf ?? "78369141005").Value!,
            Email.Create(email ?? $"{Guid.NewGuid()}@test.com").Value!,
            Phone.Create(phone ?? "11999999999").Value!
        );
    }

    [Fact]
    public async Task AddAsync_Should_Persist_Employee()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(email: "john@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(employee.Id);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(employee.Id);
        persisted.Email.Value.Should().Be("john@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Employee_When_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var result = await repository.GetByIdAsync(employee.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(employee.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Employee_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Employee_Is_Inactive_And_OnlyActive_True()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee();

        employee.Deactive();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var result = await repository.GetByIdAsync(employee.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Load_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee();

        var user = new AquaGas.Api.Modules.Auth.Domain.Models.User(
            AquaGas.Api.Modules.Auth.Domain.ValueObjects.UserName
                .Create("employeeuser").Value!,
            "hashed_password",
            AquaGas.Api.Modules.Auth.Domain.Enums.Role.Employee,
            employee.Id
        );

        db.Employees.Add(employee);
        db.Users.Add(user);

        await db.SaveChangesAsync();

        var result = await repository.GetByIdAsync(employee.Id);

        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User!.UserName.Value.Should().Be("employeeuser");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Inactive_Employee_When_OnlyActive_False()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee();

        employee.Deactive();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var result = await repository.GetByIdAsync(employee.Id, false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCPFAsync_Should_Return_Employee_When_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "cpf@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var result = await repository.GetByCPFAsync("78369141005");

        result.Should().NotBeNull();
        result!.CPF.Value.Should().Be("78369141005");
    }

    [Fact]
    public async Task AnyByCPFAsync_Should_Return_True_When_CPF_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "cpfexists@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByCPFAsync("78369141005");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyByCPFAsync_Should_Return_False_When_CPF_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var exists = await repository.AnyByCPFAsync("99999999999");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByCPFAsync_Should_Return_False_When_Employee_Is_Inactive_And_OnlyActive_True()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "inactivecpf@test.com");

        employee.Deactive();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByCPFAsync("78369141005");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByCPFAsync_Should_Return_True_When_OnlyActive_False()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "cpfinactive@test.com");

        employee.Deactive();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByCPFAsync("78369141005", false);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyByEmailAsync_Should_Return_True_When_Email_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "exists@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByEmailAsync("exists@test.com");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyByEmailAsync_Should_Return_False_When_Email_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var exists = await repository.AnyByEmailAsync("notfound@test.com");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByEmailAsync_With_IgnoreEmployeeId_Should_Ignore_Current_Employee()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "78369141005",
            email: "ignore@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByEmailAsync(
            "ignore@test.com",
            employee.Id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByEmailAsync_With_IgnoreEmployeeId_Should_Return_True_When_Email_Belongs_To_Another_Employee()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee1 = CreateEmployee(
            name: "Employee One",
            cpf: "78369141005",
            email: "shared@test.com");

        var employee2 = CreateEmployee(
            name: "Employee Two",
            cpf: "60307195058",
            email: "another@test.com");

        await repository.AddAsync(employee1);
        await repository.AddAsync(employee2);

        await repository.SaveChangesAsync();

        var exists = await repository.AnyByEmailAsync(
            "shared@test.com",
            employee2.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyByEmailAsync_Should_Return_False_When_Employee_Is_Inactive()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            cpf: "60307195058",
            email: "inactive@test.com");

        employee.Deactive();

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        var exists = await repository.AnyByEmailAsync("inactive@test.com");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyByEmailAsync_Should_Return_False_When_Email_Is_Invalid()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var result = await repository.AnyByEmailAsync(
            "invalid-email",
            Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Employees()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee1 = CreateEmployee(
            name: "Employee One",
            cpf: "60307195058",
            email: "one@test.com");

        var employee2 = CreateEmployee(
            name: "Employee Two",
            cpf: "92916240047",
            email: "two@test.com");

        await repository.AddAsync(employee1);
        await repository.AddAsync(employee2);

        await repository.SaveChangesAsync();

        var employees = await repository.GetAllAsync();

        employees.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Only_Active_Employees_By_Default()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var active = CreateEmployee(
            cpf: "92916240047",
            email: "active@test.com");

        var inactive = CreateEmployee(
            cpf: "80263235050",
            email: "inactive@test.com");

        inactive.Deactive();

        await repository.AddAsync(active);
        await repository.AddAsync(inactive);

        await repository.SaveChangesAsync();

        var result = await repository.GetAllAsync();

        result.Should().HaveCount(1);
        result.First().Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_When_OnlyActive_False()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var active = CreateEmployee(
            cpf: "80263235050",
            email: "activeall@test.com");

        var inactive = CreateEmployee(
            cpf: "90089283023",
            email: "inactiveall@test.com");

        inactive.Deactive();

        await repository.AddAsync(active);
        await repository.AddAsync(inactive);

        await repository.SaveChangesAsync();

        var result = await repository.GetAllAsync(false);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Untracked_Entities()
    {
        Guid employeeId;

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var employee = CreateEmployee();

            db.Employees.Add(employee);

            await db.SaveChangesAsync();

            employeeId = employee.Id;
        }

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var repository = CreateRepository(scope);

            var result = await repository.GetAllAsync();

            var entity = result.First(x => x.Id == employeeId);

            var tracked = db.ChangeTracker
                .Entries<EmployeeEntity>()
                .Any(x => x.Entity.Id == entity.Id);

            tracked.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Update_Should_Update_Employee_Data()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var repository = CreateRepository(scope);

        var employee = CreateEmployee(
            email: "before@test.com");

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        employee.Update(
            null,
            Email.Create("after@test.com").Value!,
            Phone.Create("11988887777").Value!
        );

        repository.Update(employee);
        await repository.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(employee.Id);

        updated.Should().NotBeNull();
        updated!.Email.Value.Should().Be("after@test.com");
        updated.Phone.Value.Should().Be("11988887777");
    }
}
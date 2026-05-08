using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

public class GetByIdEmployeeTests : BaseIntegrationTest
{
    private readonly PostgreSqlContainerFixture _fixture;

    public GetByIdEmployeeTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    private static GetByIdEmployee CreateUseCase(IServiceProvider provider)
    {
        return new GetByIdEmployee(
            provider.GetRequiredService<IEmployeeRepository>()
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Employee_With_User_Successfully()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Manager,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Employee.Name.Should().Be("John Doe");
        result.Value.Employee.Email.Should().Be("john@test.com");
        result.Value.Employee.Phone.Should().Be("11999999999");

        result.Value.User.Should().NotBeNull();

        result.Value.User!.UserName.Should().Be("john");
        result.Value.User.Role.Should().Be(Role.Manager);
    }

    [Fact]
    public async Task Execute_Should_Return_Employee_Without_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Employee.Name.Should().Be("John Doe");

        result.Value.User.Should().BeNull();
    }

    [Fact]
    public async Task Execute_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Employee not found");
    }

    [Fact]
    public async Task Execute_Should_Load_User_Navigation_Property()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value!.User.Should().NotBeNull();
        result.Value.User!.UserName.Should().Be("john");
    }

    [Fact]
    public async Task Execute_Should_Return_Correct_Employee_Id()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Employee.Id.Should().Be(employee.Id);
    }

    [Fact]
    public async Task Execute_Should_Return_Only_Requested_Employee()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee1 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var employee2 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Maria").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("maria@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        await employeeRepository.AddAsync(employee1);
        await employeeRepository.AddAsync(employee2);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee2.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Employee.Name.Should().Be("Maria");
        result.Value.Employee.Email.Should().Be("maria@test.com");
    }

    [Fact]
    public async Task Execute_Should_Not_Throw_When_Employee_Has_No_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var act = async () => await useCase.Execute(employee.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_Should_Map_User_Role_Correctly()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value!.User!.Role.Should().Be(Role.Employee);
    }
}
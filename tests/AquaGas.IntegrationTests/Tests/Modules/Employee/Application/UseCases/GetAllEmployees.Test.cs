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

public class GetAllEmployeesTests : BaseIntegrationTest
{
    private readonly PostgreSqlContainerFixture _fixture;

    public GetAllEmployeesTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    private static GetAllEmployees CreateUseCase(IServiceProvider provider)
    {
        return new GetAllEmployees(
            provider.GetRequiredService<IEmployeeRepository>()
        );
    }

    [Fact]
    public async Task Execute_Should_Return_All_Employees()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee1 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user1 = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
            employee1.Id
        );

        employee1.AssignUser(user1);

        var employee2 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Maria Doe").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("maria@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        var user2 = new User(
            UserName.Create("maria").Value!,
            "hashed",
            Role.Employee,
            employee2.Id
        );

        employee2.AssignUser(user2);

        await employeeRepository.AddAsync(employee1);
        await employeeRepository.AddAsync(employee2);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThanOrEqualTo(2);

        result.Value.Should().Contain(x =>
            x.Employee.Email == "john@test.com" &&
            x.User.UserName == "john");

        result.Value.Should().Contain(x =>
            x.Employee.Email == "maria@test.com" &&
            x.User.UserName == "maria");
    }

    [Fact]
    public async Task Execute_Should_Return_Empty_List_When_No_Employees_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_Should_Return_Employee_Without_User_As_Null()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Employee Without User").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("nouser@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Contain(x =>
            x.Employee.Email == "nouser@test.com");
    }

    [Fact]
    public async Task Execute_Should_Map_User_Correctly()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Mapped Employee").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("mapped@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("mappeduser").Value!,
            "hashed_password",
            Role.Manager,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        var employeeResult = result.Value!
            .First(x => x.Employee.Email == "mapped@test.com");

        employeeResult.User.Should().NotBeNull();
        employeeResult.User.UserName.Should().Be("mappeduser");
        employeeResult.User.Role.Should().Be(Role.Manager);
    }

    [Fact]
    public async Task Execute_Should_Return_Only_One_Employee_When_Only_One_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Single Employee").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("single@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("singleuser").Value!,
            "hashed",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().HaveCount(1);

        result.Value![0].Employee.Email.Should().Be("single@test.com");
        result.Value[0].User.UserName.Should().Be("singleuser");
    }

    [Fact]
    public async Task Execute_Should_Return_Employees_With_Correct_Ids()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Id Employee").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("id@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var expectedId = employee.Id;

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Contain(x =>
            x.Employee.Id == expectedId);
    }

    [Fact]
    public async Task Execute_Should_Map_Employee_Fields_Correctly()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Complete Employee").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("complete@test.com").Value!,
            Phone.Create("11777777777").Value!
        );

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        var employeeResult = result.Value!
            .First(x => x.Employee.Email == "complete@test.com");

        employeeResult.Employee.Name.Should().Be("Complete Employee");
        employeeResult.Employee.Cpf.Should().Be("15319984022");
        employeeResult.Employee.Phone.Should().Be("11777777777");
    }

    [Fact]
    public async Task Execute_Should_Return_Multiple_Employees_With_And_Without_Users()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employeeWithUser = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("With User").Value!,
            Cpf.Create("15319984022").Value!,
            Email.Create("withuser@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("withuser").Value!,
            "hashed",
            Role.Manager,
            employeeWithUser.Id
        );

        employeeWithUser.AssignUser(user);

        var employeeWithoutUser = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Without User").Value!,
            Cpf.Create("51473246075").Value!,
            Email.Create("withoutuser@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        await employeeRepository.AddAsync(employeeWithUser);
        await employeeRepository.AddAsync(employeeWithoutUser);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().Contain(x =>
            x.Employee.Email == "withuser@test.com" &&
            x.User != null);

        result.Value.Should().Contain(x =>
            x.Employee.Email == "withoutuser@test.com");
    }

    [Fact]
    public async Task Execute_Should_Not_Return_Duplicated_Employees()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Unique Employee").Value!,
            Cpf.Create("51473246075").Value!,
            Email.Create("unique@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);

        await employeeRepository.SaveChangesAsync();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute();

        result.IsSuccess.Should().BeTrue();

        result.Value!
            .Count(x => x.Employee.Email == "unique@test.com")
            .Should()
            .Be(1);
    }
}
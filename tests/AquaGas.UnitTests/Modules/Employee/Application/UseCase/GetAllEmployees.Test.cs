using Xunit;
using Moq;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class GetAllEmployeesTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly GetAllEmployees _useCase;

    public GetAllEmployeesTests()
    {
        _useCase = new GetAllEmployees(_repository.Object);
    }

    private Employee CreateEmployee()
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        var user = new User(
            UserName.Create("joao").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        return employee;
    }

    [Fact]
    public async Task Should_Return_Employees()
    {
        var employees = new List<Employee>
        {
            CreateEmployee(),
            CreateEmployee()
        };

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(employees);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task Should_Return_Empty_List()
    {
        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee>());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Should_Map_Correctly()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { employee });

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Id, dto.Employee.Id);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.User!.Id, dto.User.UserId);
    }
}
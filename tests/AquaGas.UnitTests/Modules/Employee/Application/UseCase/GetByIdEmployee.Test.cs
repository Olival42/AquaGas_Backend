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
using AquaGas.API.Modules.Auth.Application.Mappings;

public class GetByIdEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly GetByIdEmployee _useCase;

    public GetByIdEmployeeTests()
    {
        UserMapping.Register();
        _useCase = new GetByIdEmployee(_repository.Object);
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

        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(user, Guid.NewGuid());

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        return employee;
    }

    [Fact]
    public async Task Should_Return_Employee_When_Found()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee.Id, result.Value!.Employee.Id);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Map_User_And_Employee_Correctly()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        var dto = result.Value;

        Assert.Equal(employee.User!.Id, dto!.User.UserId);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
    }
}
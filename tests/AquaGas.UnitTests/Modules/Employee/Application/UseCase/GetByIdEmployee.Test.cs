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

    private Employee CreateEmployee(bool withUser = true)
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        if (withUser)
        {
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
        }

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

        var dto = result.Value!;

        Assert.Equal(employee.Id, dto.Employee.Id);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);

        Assert.Equal(employee.User!.Id, dto.User.UserId);
        Assert.Equal(employee.User.UserName.Value, dto.User.UserName);
    }

    [Fact]
    public async Task Should_Handle_Employee_Without_User()
    {
        var employee = CreateEmployee(withUser: false);

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.User);
    }

    [Fact]
    public async Task Should_Call_Repository_Once()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        await _useCase.Execute(employee.Id);

        _repository.Verify(x => x.GetByIdAsync(employee.Id), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Break_When_User_Has_Invalid_Data()
    {
        var employee = CreateEmployee();

        typeof(User)
            .GetProperty(nameof(User.UserName))!
            .SetValue(employee.User, null);

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Handle_Different_User_Roles()
    {
        var employee = CreateEmployee();

        typeof(User)
            .GetProperty(nameof(User.Role))!
            .SetValue(employee.User, Role.Manager);

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        Assert.Equal(Role.Manager.ToString(), result.Value!.User.Role.ToString());
    }

    [Fact]
    public async Task Should_Not_Call_Update_Or_Save()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        await _useCase.Execute(employee.Id);

        _repository.Verify(x => x.Update(It.IsAny<Employee>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Keep_Same_Ids_Between_Domain_And_Response()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(employee.Id);

        Assert.Equal(employee.Id, result.Value!.Employee.Id);
        Assert.Equal(employee.User!.Id, result.Value.User.UserId);
    }

    [Fact]
    public async Task Should_Execute_Quickly()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        var start = DateTime.UtcNow;

        await _useCase.Execute(employee.Id);

        var duration = DateTime.UtcNow - start;

        Assert.True(duration.TotalMilliseconds < 100);
    }
}
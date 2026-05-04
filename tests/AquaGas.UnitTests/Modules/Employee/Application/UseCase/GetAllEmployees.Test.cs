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

    private Employee CreateEmployeeWithUser()
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

    private Employee CreateEmployeeWithoutUser()
    {
        return new Employee(
            EmployeeName.Create("Maria").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("maria@email.com").Value!,
            Phone.Create("44988888888").Value!
        );
    }

    [Fact]
    public async Task Should_Return_Employees()
    {
        var employees = new List<Employee>
        {
            CreateEmployeeWithUser(),
            CreateEmployeeWithUser()
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
    public async Task Should_Handle_Employees_Without_User()
    {
        var employees = new List<Employee>
        {
            CreateEmployeeWithoutUser()
        };

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(employees);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);

        var dto = result.Value!.First();
        Assert.NotNull(dto.Employee);
        Assert.Null(dto.User); // comportamento esperado se User for null
    }

    [Fact]
    public async Task Should_Map_Correctly_When_User_Exists()
    {
        var employee = CreateEmployeeWithUser();

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { employee });

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Id, dto.Employee.Id);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);

        Assert.NotNull(dto.User);
        Assert.Equal(employee.User!.UserName.Value, dto.User.UserName);
        Assert.Equal(employee.User.Role.ToString(), dto.User.Role.ToString());
    }

    [Fact]
    public async Task Should_Call_Repository_Once()
    {
        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee>());

        await _useCase.Execute();

        _repository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Handle_Null_List_From_Repository()
    {
        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync((List<Employee>?)null!);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Should_Map_Multiple_Different_Employees_Correctly()
    {
        var emp1 = CreateEmployeeWithUser();
        var emp2 = CreateEmployeeWithUser();

        typeof(Employee)
            .GetProperty(nameof(Employee.Name))!
            .SetValue(emp2, EmployeeName.Create("Maria").Value!);

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { emp1, emp2 });

        var result = await _useCase.Execute();

        Assert.Equal(2, result.Value!.Count);
        Assert.NotEqual(result.Value[0].Employee.Name, result.Value[1].Employee.Name);
    }

    [Fact]
    public async Task Should_Keep_Ids_Consistent_Between_Entity_And_Response()
    {
        var employee = CreateEmployeeWithUser();

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { employee });

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Id, dto.Employee.Id);
    }

    [Fact]
    public async Task Should_Not_Break_When_User_Has_Invalid_Data()
    {
        var employee = CreateEmployeeWithUser();

        typeof(User)
            .GetProperty(nameof(User.UserName))!
            .SetValue(employee.User, null);

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { employee });

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Call_Repository_Multiple_Times()
    {
        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee>());

        await _useCase.Execute();

        _repository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Map_All_Fields_Correctly()
    {
        var employee = CreateEmployeeWithUser();

        _repository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Employee> { employee });

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);
        Assert.Equal(employee.Phone.Value, dto.Employee.Phone);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);

        Assert.Equal(employee.User!.UserName.Value, dto.User.UserName);
    }
}
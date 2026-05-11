using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Domain.Models;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using Moq;
using Xunit;

public class GetAllEmployeesTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    private readonly GetAllEmployees _useCase;

    public GetAllEmployeesTests()
    {
        _useCase = new GetAllEmployees(
            _employeeRepository.Object,
            _userRepository.Object
        );
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    private static User CreateUser(Employee employee)
    {
        var user = new User(
            UserName.Create("joao").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );

        employee.AssignUserId(user.Id);

        return user;
    }

    [Fact]
    public async Task Should_Return_Employees()
    {
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();

        var user1 = CreateUser(employee1);
        var user2 = CreateUser(employee2);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee>
            {
                employee1,
                employee2
            });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee1.Id))
            .ReturnsAsync(user1);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee2.Id))
            .ReturnsAsync(user2);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task Should_Return_Empty_List()
    {
        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee>());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Should_Ignore_Employees_Without_User()
    {
        var employee = CreateEmployee();

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee> { employee });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync((User?)null);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Should_Map_Correctly_When_User_Exists()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee> { employee });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Id, dto.Employee.Id);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);

        Assert.NotNull(dto.User);
        Assert.Equal(user.UserName.Value, dto.User.UserName);
        Assert.Equal(user.Role.ToString(), dto.User.Role.ToString());
    }

    [Fact]
    public async Task Should_Call_Repository_Once()
    {
        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee>());

        await _useCase.Execute();

        _employeeRepository.Verify(
            x => x.GetAllAsync(true),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Handle_Null_List_From_Repository()
    {
        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync((IEnumerable<Employee>?)null!);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Should_Map_Multiple_Different_Employees_Correctly()
    {
        var emp1 = CreateEmployee();

        var emp2 = new Employee(
            EmployeeName.Create("Maria").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("maria@email.com").Value!,
            Phone.Create("44988888888").Value!
        );

        var user1 = CreateUser(emp1);

        var user2 = new User(
            UserName.Create("maria").Value!,
            "hash",
            Role.Employee,
            emp2.Id
        );

        emp2.AssignUserId(user2.Id);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee> { emp1, emp2 });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(emp1.Id))
            .ReturnsAsync(user1);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(emp2.Id))
            .ReturnsAsync(user2);

        var result = await _useCase.Execute();

        Assert.Equal(2, result.Value!.Count);

        Assert.NotEqual(
            result.Value[0].Employee.Name,
            result.Value[1].Employee.Name
        );
    }

    [Fact]
    public async Task Should_Keep_Ids_Consistent_Between_Entity_And_Response()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee> { employee });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Id, dto.Employee.Id);
    }

    [Fact]
    public async Task Should_Not_Call_Repository_Multiple_Times()
    {
        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee>());

        await _useCase.Execute();

        _employeeRepository.Verify(
            x => x.GetAllAsync(true),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Map_All_Fields_Correctly()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee> { employee });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);
        Assert.Equal(employee.Phone.Value, dto.Employee.Phone);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);

        Assert.Equal(user.UserName.Value, dto.User.UserName);
    }

    [Fact]
    public async Task Should_Call_UserRepository_For_Each_Employee()
    {
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();

        var user1 = CreateUser(employee1);
        var user2 = CreateUser(employee2);

        _employeeRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(new List<Employee>
            {
                employee1,
                employee2
            });

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee1.Id))
            .ReturnsAsync(user1);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee2.Id))
            .ReturnsAsync(user2);

        await _useCase.Execute();

        _userRepository.Verify(
            x => x.GetByEmployeeIdAsync(It.IsAny<Guid>()),
            Times.Exactly(2)
        );
    }
}
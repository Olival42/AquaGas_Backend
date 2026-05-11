using Xunit;
using Moq;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Domain.Models;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Auth.Application.Mappings;

public class GetByIdEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    private readonly GetByIdEmployee _useCase;

    public GetByIdEmployeeTests()
    {
        UserMapping.Register();

        _useCase = new GetByIdEmployee(
            _repository.Object,
            _userRepository.Object
        );
    }

    private Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    private User CreateUser(Employee employee)
    {
        return new User(
            UserName.Create("joao").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );
    }

    [Fact]
    public async Task Should_Return_Employee_When_Found()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee.Id, result.Value!.Employee.Id);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Map_User_And_Employee_Correctly()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        var dto = result.Value!;

        Assert.Equal(employee.Id, dto.Employee.Id);
        Assert.Equal(employee.Name.Value, dto.Employee.Name);
        Assert.Equal(employee.Email.Value, dto.Employee.Email);
        Assert.Equal(employee.CPF.Value, dto.Employee.Cpf);

        Assert.Equal(user.Id, dto.User.UserId);
        Assert.Equal(user.UserName.Value, dto.User.UserName);
    }

    [Fact]
    public async Task Should_Handle_Employee_Without_User()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync((User?)null);

        var result = await _useCase.Execute(employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Call_Repository_Once()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(employee.Id);

        _repository.Verify(
            x => x.GetByIdAsync(employee.Id, true),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Not_Break_When_User_Has_Invalid_Data()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        typeof(User)
            .GetProperty(nameof(User.UserName))!
            .SetValue(user, null);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Handle_Different_User_Roles()
    {
        var employee = CreateEmployee();

        var user = new User(
            UserName.Create("manager").Value!,
            "hash",
            Role.Manager,
            employee.Id
        );

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.Equal(
            Role.Manager.ToString(),
            result.Value!.User.Role.ToString()
        );
    }

    [Fact]
    public async Task Should_Keep_Same_Ids_Between_Domain_And_Response()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.Equal(employee.Id, result.Value!.Employee.Id);
        Assert.Equal(user.Id, result.Value.User.UserId);
    }

    [Fact]
    public async Task Should_Execute_Quickly()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var start = DateTime.UtcNow;

        await _useCase.Execute(employee.Id);

        var duration = DateTime.UtcNow - start;

        Assert.True(duration.TotalMilliseconds < 100);
    }
}
using Xunit;
using Moq;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Domain.Models;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Shared.Results;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Application.Services;
using AquaGas.Auth.Domain.Services;

public class UpdateEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private readonly UpdateEmployee _useCase;

    public UpdateEmployeeTests()
    {
        _useCase = new UpdateEmployee(
            _repository.Object,
            _audit.Object,
            _userContext.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _userRepository.Object
        );

        SetupBase();
    }

    private void SetupBase()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _userRepository.Setup(x =>
                x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _employeeRepositorySetup();

        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
    }

    private void _employeeRepositorySetup()
    {
        _repository.Setup(x =>
                x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);
    }

    private Employee CreateEmployee(Role role = Role.Employee)
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        var user = new User(
            UserName.Create("joao").Value!,
            "hash",
            role,
            employee.Id
        );

        employee.AssignUserId(user.Id);

        return employee;
    }

    private User CreateUser(Employee employee, Role role = Role.Employee)
    {
        return new User(
            UserName.Create("joao").Value!,
            "hash",
            role,
            employee.Id
        );
    }

    [Fact]
    public async Task Should_Update_Employee()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(input, employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria", result.Value!.Employee.Name);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(
            new UpdateEmployeeInput(),
            Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync((User?)null);

        var result = await _useCase.Execute(
            new UpdateEmployeeInput(),
            employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_UserName_Already_Exists()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            UserName = "joao123"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        _userRepository.Setup(x =>
            x.AnyByUserNameAsync(input.UserName!, user.Id))
        .ReturnsAsync(true);

        var result = await _useCase.Execute(input, employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "User name already registered",
            result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_UserName_Changes()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            UserName = "novoUser"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_Password_Changes()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            NewPassword = "Senha@123"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Call_Audit_Log()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(new UpdateEmployeeInput(), employee.Id);

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            "admin",
            AuditAction.UPDATE,
            "Employee",
            employee.Id,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(new UpdateEmployeeInput(), employee.Id);

        _repository.Verify(x => x.Update(employee), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Revoke_Tokens_When_No_Changes()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            Role = Role.Employee.ToString()
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Fails()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(
            new UpdateEmployeeInput(),
            employee.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Validation_Fails()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput
        {
            Email = "email-invalido"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(input, employee.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_Role_Changes()
    {
        var employee = CreateEmployee(Role.Employee);
        var user = CreateUser(employee, Role.Employee);

        var input = new UpdateEmployeeInput
        {
            Role = "Manager"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Not_Check_UserName_When_Null()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee);

        var input = new UpdateEmployeeInput();

        _repository.Setup(x => x.GetByIdAsync(employee.Id, true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(input, employee.Id);

        _userRepository.Verify(
            x => x.AnyByUserNameAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>()),
            Times.Never
        );
    }
}
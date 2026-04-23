using Xunit;
using Moq;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Shared.Errors;

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
    }

    private Employee CreateEmployee(Role role = Role.Employee)
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        var user = new User(
            UserName.Create("joao").Value!,
            "Pasword@123",
            role,
            employee.Id
        );

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        return employee;
    }

    [Fact]
    public async Task Should_Update_Employee()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _useCase.Execute(input, employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria", result.Value!.Employee.Name);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(new UpdateEmployeeInput(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_UserName_Already_Exists()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput
        {
            UserName = "joao123"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.AnyByUserNameAsync(input.UserName!, employee.User!.Id))
            .ReturnsAsync(true);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _useCase.Execute(input, employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("User name already registered", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_UserName_Changes()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput
        {
            UserName = "novoUser"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>(), employee.User!.Id))
            .ReturnsAsync(false);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(employee.User!.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_Password_Changes()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput
        {
            NewPassword = "Senha@123"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("new_hash");

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(employee.User!.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Call_Audit_Log()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(new UpdateEmployeeInput(), employee.Id);

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            null,
            AuditAction.UPDATE,
            It.IsAny<string>(),
            employee.Id,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(new UpdateEmployeeInput(), employee.Id);

        _repository.Verify(x => x.Update(employee), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Revoke_Tokens_When_No_Changes()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

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

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(new UpdateEmployeeInput(), employee.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Validation_Fails()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput
        {
            Email = "email-invalido"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _useCase.Execute(input, employee.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_Role_Changes()
    {
        var employee = CreateEmployee(Role.Employee);

        var input = new UpdateEmployeeInput
        {
            Role = "Manager"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(input, employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(employee.User!.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Not_Check_UserName_When_Null()
    {
        var employee = CreateEmployee();

        var input = new UpdateEmployeeInput();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _useCase.Execute(input, employee.Id);

        _userRepository.Verify(
            x => x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never
        );
    }
}
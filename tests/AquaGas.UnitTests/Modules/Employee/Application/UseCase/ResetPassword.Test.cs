using Xunit;
using Moq;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Domain.Enums;

public class ResetPasswordTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IRefreshTokenRepository> _refresh = new();
    private readonly Mock<IUserContextService> _userContext = new();

    private readonly ResetPassword _useCase;

    public ResetPasswordTests()
    {
        _useCase = new ResetPassword(
            _repository.Object,
            _hasher.Object,
            _audit.Object,
            _refresh.Object,
            _userContext.Object
        );
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
    public async Task Should_Reset_Password()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed");

        var input = new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        };

        var result = await _useCase.Execute(employee.Id, input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid(), new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Revoke_Tokens()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed");

        await _useCase.Execute(employee.Id, new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        });

        _refresh.Verify(
            x => x.RevokeAllByUserId(employee.User!.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed");

        await _useCase.Execute(employee.Id, new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        });

        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Call_Audit_Log()
    {
        var employee = CreateEmployee();

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed");

        await _useCase.Execute(employee.Id, new ResetPasswordInput
        {
            NewPassword = "Senha@123"
        });

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            null,
            It.IsAny<AuditAction>(),
            "Employee",
            employee.Id,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }
}
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

public class UpdateEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private readonly UpdateEmployee _useCase;

    public UpdateEmployeeTests()
    {
        _useCase = new UpdateEmployee(
            _repository.Object,
            _audit.Object,
            _userContext.Object,
            _refreshTokenRepository.Object
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
            "hash",
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
        var userId = Guid.NewGuid();

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888"
        };

        _repository.Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        var result = await _useCase.Execute(input, employee.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria", result.Value!.Employee.Name);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Revoke_Tokens_When_Role_Changes()
    {
        var employee = CreateEmployee(Role.Employee);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888",
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
    public async Task Should_Not_Revoke_Tokens_When_Role_Does_Not_Change()
    {
        var employee = CreateEmployee(Role.Employee);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria",
            Email = "maria@email.com",
            Phone = "44988888888",
            Role = "Employee"
        };

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
    public async Task Should_Call_Audit_Log()
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

        await _useCase.Execute(input, employee.Id);

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

        await _useCase.Execute(input, employee.Id);

        _repository.Verify(x => x.Update(employee), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
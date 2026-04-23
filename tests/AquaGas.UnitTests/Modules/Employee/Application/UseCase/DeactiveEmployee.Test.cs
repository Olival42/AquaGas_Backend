using Xunit;
using Moq;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Modules.Auth.Domain.Repositories;

public class DeactiveEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private readonly DeactiveEmployee _useCase;

    public DeactiveEmployeeTests()
    {
        _useCase = new DeactiveEmployee(
            _repository.Object,
            _userContext.Object,
            _audit.Object,
            _refreshTokenRepository.Object
        );
    }

    private Employee CreateEmployee(Guid userId)
    {
        var employee = new Employee(
            EmployeeName.Create("João Silva").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        var user = new User(
            UserName.Create("joao").Value!,
            "hashed-password",
            Role.Employee,
            employee.Id
        );

        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(user, userId);

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        return employee;
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Not_Found()
    {
        var id = Guid.NewGuid();

        _repository.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Deactivating_Self()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(userId);

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Deactivate_Employee()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        await _useCase.Execute(Guid.NewGuid());

        _repository.Verify(x => x.Update(It.IsAny<Employee>()), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Revoke_Refresh_Tokens()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        await _useCase.Execute(Guid.NewGuid());

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        await _useCase.Execute(Guid.NewGuid());

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }
}
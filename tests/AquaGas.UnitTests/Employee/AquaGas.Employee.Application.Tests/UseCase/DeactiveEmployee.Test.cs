using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Application.UseCases;
using AquaGas.Employee.Domain.Models;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Moq;
using Xunit;

public class DeactiveEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private readonly DeactiveEmployee _useCase;

    private readonly Guid _currentUserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly string _currentUserName = "admin";

    public DeactiveEmployeeTests()
    {
        _useCase = new DeactiveEmployee(
            _employeeRepository.Object,
            _userContext.Object,
            _audit.Object,
            _refreshTokenRepository.Object,
            _userRepository.Object
        );

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(_currentUserId));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success(_currentUserName));
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("João Silva").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    private static User CreateUser(Employee employee, Guid? userId = null)
    {
        var user = new User(
            UserName.Create("joao").Value!,
            "hashed-password",
            Role.Employee,
            employee.Id
        );

        if (userId.HasValue)
        {
            typeof(User)
                .GetProperty(nameof(User.Id))!
                .SetValue(user, userId.Value);
        }

        employee.AssignUserId(user.Id);

        return user;
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Not_Found()
    {
        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_User_Not_Found()
    {
        var employee = CreateEmployee();

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync((User?)null);

        var result = await _useCase.Execute(employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("User not found"));
    }

    [Fact]
    public async Task Should_Fail_When_Deactivating_Self()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, _currentUserId);

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            result.Errors,
            e => e.Message.Contains("You cannot deactivate")
        );
    }

    [Fact]
    public async Task Should_Deactivate_Employee()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, Guid.NewGuid());

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, Guid.NewGuid());

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(employee.Id);

        _employeeRepository.Verify(x => x.Update(employee), Times.Once);

        _employeeRepository.Verify(x => x.SaveChangesAsync(), Times.Once);

        _userRepository.Verify(x => x.Update(user), Times.Once);

        _userRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Revoke_Refresh_Tokens()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, Guid.NewGuid());

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(employee.Id);

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, Guid.NewGuid());

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        await _useCase.Execute(employee.Id);

        _audit.Verify(x => x.LogAsync(
            _currentUserId,
            _currentUserName,
            AuditAction.DEACTIVATE,
            "Employee",
            employee.Id,
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Id_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                Error.Unauthorized("Invalid user")
            ));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Username_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(
                Error.Unauthorized("Invalid user")
            ));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Return_Success_Result_Object()
    {
        var employee = CreateEmployee();
        var user = CreateUser(employee, Guid.NewGuid());

        _employeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), true))
            .ReturnsAsync(employee);

        _userRepository.Setup(x => x.GetByEmployeeIdAsync(employee.Id))
            .ReturnsAsync(user);

        var result = await _useCase.Execute(employee.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
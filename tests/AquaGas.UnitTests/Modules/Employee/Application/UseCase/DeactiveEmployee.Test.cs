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
using AquaGas.Api.Shared.Errors;

public class DeactiveEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private readonly DeactiveEmployee _useCase;

    private readonly Guid _currentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly string _currentUserName = "admin";

    public DeactiveEmployeeTests()
    {
        _useCase = new DeactiveEmployee(
            _repository.Object,
            _userContext.Object,
            _audit.Object,
            _refreshTokenRepository.Object
        );

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(_currentUserId));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success(_currentUserName));
    }

    private Employee CreateEmployee(Guid? userId = null)
    {
        var employee = new Employee(
            EmployeeName.Create("João Silva").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        if (userId is not null)
        {
            var user = new User(
                UserName.Create("joao").Value!,
                "hashed-password",
                Role.Employee,
                employee.Id
            );

            typeof(User)
                .GetProperty(nameof(User.Id))!
                .SetValue(user, userId.Value);

            typeof(Employee)
                .GetProperty(nameof(Employee.User))!
                .SetValue(employee, user);
        }

        return employee;
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Not_Found()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Deactivating_Self()
    {
        var employee = CreateEmployee(_currentUserId);

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("You cannot deactivate"));
    }

    [Fact]
    public async Task Should_Deactivate_Employee()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task Should_Save_Changes()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        await _useCase.Execute(Guid.NewGuid());

        _repository.Verify(x => x.Update(employee), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Revoke_Refresh_Tokens_When_User_Exists()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        await _useCase.Execute(Guid.NewGuid());

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(employee.User!.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Not_Revoke_Refresh_Tokens_When_User_Is_Null()
    {
        var employee = CreateEmployee(); // sem user

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        await _useCase.Execute(Guid.NewGuid());

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        await _useCase.Execute(Guid.NewGuid());

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
    public async Task Should_Fail_When_UserContext_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Id_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Username_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Revoke_Tokens_When_Employee_Has_No_User()
    {
        var employee = new Employee(
            EmployeeName.Create("João Silva").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(Guid.NewGuid());

        _refreshTokenRepository.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Call_Update_On_Repository()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(Guid.NewGuid());

        _repository.Verify(x => x.Update(employee), Times.Once);
    }

    [Fact]
    public async Task Should_Set_Employee_As_Inactive()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(Guid.NewGuid());

        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task Should_Return_Success_Result_Object()
    {
        var employee = CreateEmployee(Guid.NewGuid());

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(employee);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}
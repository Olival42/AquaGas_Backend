using Xunit;
using Moq;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public class RegisterEmployeeTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IUserContextService> _userContext = new();

    private readonly RegisterEmployee _useCase;

    public RegisterEmployeeTests()
    {
        _useCase = new RegisterEmployee(
            _employeeRepo.Object,
            _userRepo.Object,
            _hasher.Object,
            _audit.Object,
            _userContext.Object
        );

        SetupBase();
    }

    private void SetupBase()
    {
        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.GetByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync((Employee?)null);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
    }

    private RegisterEmployeeInput ValidInput() =>
        new()
        {
            Employee = new()
            {
                Name = "João Silva",
                Cpf = "79522375004",
                Email = "joao@email.com",
                Phone = "44999999999"
            },
            User = new()
            {
                UserName = "joao",
                Password = "Senha@123",
                Role = "Manager"
            }
        };

    [Fact]
    public async Task Should_Register_Employee()
    {
        var result = await _useCase.Execute(ValidInput());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Cpf_Exists()
    {
        _employeeRepo.Setup(x => x.GetByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(new Employee(
                EmployeeName.Create("João").Value!,
                Cpf.Create("79522375004").Value!,
                Email.Create("a@a.com").Value!,
                Phone.Create("99999999999").Value!
            ));

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
        Assert.Equal("CPF already registered", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Fail_When_Email_Exists()
    {
        _employeeRepo.Setup(x =>
                x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Username_Exists()
    {
        _userRepo.Setup(x =>
                x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Hash_Password()
    {
        await _useCase.Execute(ValidInput());

        _hasher.Verify(x => x.Hash("Senha@123"), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Repositories()
    {
        await _useCase.Execute(ValidInput());

        _employeeRepo.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Once);
        _employeeRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        await _useCase.Execute(ValidInput());

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            "joao",
            AuditAction.CREATE,
            "Employee",
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_UserContext_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Hash_Password_When_Email_Already_Exists()
    {
        _employeeRepo.Setup(x =>
                x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Reactivate_Existing_Inactive_Employee()
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("a@a.com").Value!,
            Phone.Create("99999999999").Value!
        );

        employee.Deactive();

        var user = new User(
            UserName.Create("joao").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );

        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(user, Guid.NewGuid());

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        _employeeRepo.Setup(x => x.GetByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(employee);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var result = await _useCase.Execute(ValidInput());

        Assert.True(result.IsSuccess);
        Assert.True(employee.IsActive);
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Already_Active()
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("a@a.com").Value!,
            Phone.Create("99999999999").Value!
        );

        _employeeRepo.Setup(x => x.GetByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(employee);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Return_Correct_Response_Structure()
    {
        var result = await _useCase.Execute(ValidInput());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Employee);
        Assert.NotNull(result.Value!.User);
    }

    [Fact]
    public async Task Should_Fail_When_UserName_Context_Is_Invalid()
    {
        SetupBase();

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(Error.Unauthorized("Invalid username")));

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Not_Hash_When_UserContext_Fails()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("Invalid user")));

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Create_When_Email_And_Username_Conflict()
    {
        _employeeRepo.Setup(x =>
                x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _userRepo.Setup(x =>
                x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);

        _employeeRepo.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task Should_Call_GetByCPF_Only_Once()
    {
        SetupBase();

        await _useCase.Execute(ValidInput());

        _employeeRepo.Verify(x => x.GetByCPFAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Should_Create_User_With_Correct_Role()
    {
        SetupBase();

        User? capturedUser = null;

        _employeeRepo
            .Setup(x => x.AddAsync(It.IsAny<Employee>()))
            .Callback<Employee>(e => capturedUser = e.User);

        await _useCase.Execute(ValidInput());

        Assert.NotNull(capturedUser);
        Assert.Equal(Role.Manager, capturedUser!.Role);
    }

    [Fact]
    public async Task Should_Not_Persist_Plain_Password()
    {
        SetupBase();

        await _useCase.Execute(ValidInput());

        _hasher.Verify(x => x.Hash("Senha@123"), Times.Once);
    }

    [Fact]
    public async Task Should_Reactivate_User_When_Employee_Has_User()
    {
        var employee = new Employee(
            EmployeeName.Create("João").Value!,
            Cpf.Create("79522375004").Value!,
            Email.Create("a@a.com").Value!,
            Phone.Create("99999999999").Value!
        );

        employee.Deactive();

        var user = new User(
            UserName.Create("old").Value!,
            "hash",
            Role.Employee,
            employee.Id
        );

        typeof(User)
            .GetProperty(nameof(User.Id))!
            .SetValue(user, Guid.NewGuid());

        typeof(Employee)
            .GetProperty(nameof(Employee.User))!
            .SetValue(employee, user);

        _employeeRepo.Setup(x => x.GetByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(employee);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var result = await _useCase.Execute(ValidInput());

        Assert.True(result.IsSuccess);
        Assert.True(employee.User!.IsActive);
    }
}
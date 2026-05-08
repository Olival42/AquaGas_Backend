using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Employee.Application.Mappings;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class UpdateEmployeeTests : BaseIntegrationTest
{

    private readonly PostgreSqlContainerFixture _fixture;
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
    private readonly Mock<IUserContextService> _userContextServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    public UpdateEmployeeTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        EmployeeMapping.Register();
        _fixture = fixture;
    }

    private UpdateEmployee CreateUseCase(IServiceProvider provider)
    {
        return new UpdateEmployee(
            provider.GetRequiredService<IEmployeeRepository>(),
            _auditLogServiceMock.Object,
            _userContextServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _passwordHasherMock.Object,
            provider.GetRequiredService<IUserRepository>()
        );
    }

    [Fact]
    public async Task Execute_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated",
            Email = "johnupdated@test.com",
            Phone = "11888888888",
            UserName = "johnupdated",
            Role = nameof(Role.Manager),
            NewPassword = "John@1234"
        };

        var result = await useCase.Execute(input, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Message == "Employee not found");
    }

    [Fact]
    public async Task Execute_Should_Update_Employee_Successfully()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("new_hashed_password");

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated",
            Email = "johnupdated@test.com",
            Phone = "11888888888",
            UserName = "johnupdated",
            Role = nameof(Role.Manager),
            NewPassword = "John@1234"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Employee.Name.Should().Be("John Updated");
        result.Value.Employee.Email.Should().Be("johnupdated@test.com");
        result.Value.Employee.Phone.Should().Be("11888888888");

        result.Value.User.UserName.Should().Be("johnupdated");
        result.Value.User.Role.Should().Be(Role.Manager);

        var persisted = await employeeRepository.GetByIdAsync(employee.Id);

        persisted.Should().NotBeNull();

        persisted!.Name.Value.Should().Be("John Updated");
        persisted.Email.Value.Should().Be("johnupdated@test.com");
        persisted.Phone.Value.Should().Be("11888888888");

        persisted.User!.UserName.Value.Should().Be("johnupdated");
        persisted.User.Role.Should().Be(Role.Manager);

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once);

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "admin",
                AuditAction.UPDATE,
                "Employee",
                employee.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_Email_Already_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee1 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var employee2 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Maria").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("maria@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        var user = new User(
            UserName.Create("maria").Value!,
            "hashed_password",
            Role.Employee,
            employee2.Id
        );

        employee2.AssignUser(user);

        await employeeRepository.AddAsync(employee1);
        await employeeRepository.AddAsync(employee2);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria Updated",
            Email = "john@test.com",
            Phone = "11888888888",
            UserName = "mariaupdated",
            Role = nameof(Role.Manager),
            NewPassword = "John@1234"
        };

        var result = await useCase.Execute(input, employee2.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Email already registered");
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_UserName_Already_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee1 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var employee2 = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Maria").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("maria@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        var user1 = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee1.Id
        );

        var user2 = new User(
            UserName.Create("maria").Value!,
            "hashed_password",
            Role.Employee,
            employee2.Id
        );

        employee1.AssignUser(user1);
        employee2.AssignUser(user2);

        await employeeRepository.AddAsync(employee1);
        await employeeRepository.AddAsync(employee2);

        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "Maria Updated",
            Email = "mariaupdated@test.com",
            Phone = "11888888888",
            UserName = "john",
            Role = nameof(Role.Manager),
            NewPassword = "John@1234"
        };

        var result = await useCase.Execute(input, employee2.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "User name already registered");
    }

    [Fact]
    public async Task Execute_Should_Return_Error_When_Employee_Has_No_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated",
            Email = "johnupdated@test.com",
            Phone = "11888888888",
            UserName = "johnupdated",
            Role = nameof(Role.Manager),
            NewPassword = "John@1234"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Employee has no associated user");
    }

    [Fact]
    public async Task Execute_Should_Not_Revoke_RefreshTokens_When_Credentials_Do_Not_Change()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated",
            Email = "johnupdated@test.com",
            Phone = "11888888888"
        }; ;

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserContext_UserId_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                AquaGas.Api.Shared.Errors.Error.Unauthorized("Unauthorized")));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserContext_UserName_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(
                AquaGas.Api.Shared.Errors.Error.Unauthorized("Unauthorized")));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Validation_Error_When_Input_Is_Invalid()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Email = "invalid-email",
            Phone = "123",
            Role = "INVALID_ROLE"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Execute_Should_Update_Only_Employee_Data_When_User_Data_Is_Not_Provided()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Name = "John Updated",
            Email = "johnupdated@test.com",
            Phone = "11888888888"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        var persisted = await employeeRepository.GetByIdAsync(employee.Id);

        persisted!.Name.Value.Should().Be("John Updated");
        persisted.Email.Value.Should().Be("johnupdated@test.com");
        persisted.Phone.Value.Should().Be("11888888888");

        persisted.User!.UserName.Value.Should().Be("john");
        persisted.User.Role.Should().Be(Role.Employee);

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Revoke_RefreshTokens_When_Only_UserName_Changes()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            UserName = "johnupdated"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Revoke_RefreshTokens_When_Only_Role_Changes()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            Role = nameof(Role.Manager)
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Revoke_RefreshTokens_When_Only_Password_Changes()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed_password",
            Role.Employee,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("new_hash");

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new UpdateEmployeeInput
        {
            NewPassword = "John@123"
        };

        var result = await useCase.Execute(input, employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once);
    }
}
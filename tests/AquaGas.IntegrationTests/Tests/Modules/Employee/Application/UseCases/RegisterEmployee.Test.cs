using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
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
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Employee.Application.Mappings;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class RegisterEmployeeTests : BaseIntegrationTest
{
    private readonly PostgreSqlContainerFixture _fixture;

    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
    private readonly Mock<IUserContextService> _userContextServiceMock = new();

    public RegisterEmployeeTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        EmployeeMapping.Register();
        _fixture = fixture;
    }

    private RegisterEmployee CreateUseCase(IServiceProvider provider)
    {
        return new RegisterEmployee(
            provider.GetRequiredService<IEmployeeRepository>(),
            provider.GetRequiredService<IUserRepository>(),
            _passwordHasherMock.Object,
            _auditLogServiceMock.Object,
            _userContextServiceMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Register_Employee_Successfully()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "71447091000",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Employee.Name.Should().Be("John Doe");
        result.Value.Employee.Email.Should().Be("john@test.com");

        result.Value.User.UserName.Should().Be("john");
        result.Value.User.Role.Should().Be(Role.Manager);

        var persisted =
            await employeeRepository.GetByCPFAsync("71447091000");

        persisted.Should().NotBeNull();

        persisted!.User.Should().NotBeNull();
        persisted.User!.UserName.Value.Should().Be("john");

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "john",
                AuditAction.CREATE,
                "Employee",
                persisted.Id,
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_CPF_Already_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
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

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "71447091000",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "CPF already registered");
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_Email_Already_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
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

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "98765432100",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Email already registered");
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_Username_Already_Exists()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
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

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "11122233396",
                Email = "another@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Username already registered");
    }

    [Fact]
    public async Task Execute_Should_Reactivate_Employee_When_Employee_Is_Inactive()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Old Name").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("old@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        employee.Deactive();

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "71447091000",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("71447091000");

        persisted.Should().NotBeNull();

        persisted!.IsActive.Should().BeTrue();
        persisted.Name.Value.Should().Be("John Doe");
        persisted.Email.Value.Should().Be("john@test.com");

        persisted.User.Should().NotBeNull();

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "john",
                AuditAction.UPDATE,
                "Employee",
                persisted.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserContext_UserId_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                Error.Unauthorized("Unauthorized")));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "71447091000",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserContext_UserName_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(
                Error.Unauthorized("Unauthorized")));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Doe",
                Cpf = "71447091000",
                Email = "john@test.com",
                Phone = "11999999999"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@1234",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Validation_Error_When_Input_Is_Invalid()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "",
                Cpf = "123",
                Email = "invalid-email",
                Phone = "1"
            },
            User = new UserInput
            {
                UserName = "",
                Password = "123",
                Role = "INVALID"
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Execute_Should_Not_Persist_Employee_When_Validation_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "",
                Cpf = "123",
                Email = "invalid-email",
                Phone = "1"
            },
            User = new UserInput
            {
                UserName = "",
                Password = "123",
                Role = "INVALID"
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("123");

        persisted.Should().BeNull();
    }

    [Fact]
    public async Task Execute_Should_Not_Create_Employee_When_Email_Already_Exists()
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

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Maria",
                Cpf = "98765432100",
                Email = "john@test.com",
                Phone = "11888888888"
            },
            User = new UserInput
            {
                UserName = "maria",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("98765432100");

        persisted.Should().BeNull();

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Not_Create_Employee_When_Username_Already_Exists()
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
            "hashed",
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

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Maria",
                Cpf = "98765432100",
                Email = "maria@test.com",
                Phone = "11888888888"
            },
            User = new UserInput
            {
                UserName = "john",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("98765432100");

        persisted.Should().BeNull();

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Keep_Same_Id_When_Reactivating_Employee()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Old Name").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("old@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var originalId = employee.Id;

        employee.Deactive();

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Updated",
                Cpf = "12345678909",
                Email = "updated@test.com",
                Phone = "11888888888"
            },
            User = new UserInput
            {
                UserName = "johnupdated",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("12345678909");

        persisted.Should().NotBeNull();

        persisted!.Id.Should().Be(originalId);
    }

    [Fact]
    public async Task Execute_Should_Create_User_When_Reactivating_Employee_Without_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Old Name").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("old@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        employee.Deactive();

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "John Updated",
                Cpf = "12345678909",
                Email = "updated@test.com",
                Phone = "11888888888"
            },
            User = new UserInput
            {
                UserName = "newuser",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByCPFAsync("12345678909");

        persisted.Should().NotBeNull();

        persisted!.User.Should().NotBeNull();

        persisted.User!.UserName.Value.Should().Be("newuser");
        persisted.User.Role.Should().Be(Role.Manager);
        persisted.User.IsActive.Should().BeTrue();

        _passwordHasherMock.Verify(
            x => x.Hash("John@123"),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_Reactivating_With_Email_Already_Registered()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var activeEmployee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Active").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("active@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        var inactiveEmployee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Inactive").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("inactive@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        inactiveEmployee.Deactive();

        await employeeRepository.AddAsync(activeEmployee);
        await employeeRepository.AddAsync(inactiveEmployee);

        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Reactivated",
                Cpf = "12345678909",
                Email = "active@test.com",
                Phone = "11777777777"
            },
            User = new UserInput
            {
                UserName = "reactivated",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Email already registered");
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_Reactivating_With_Username_Already_Registered()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var activeEmployee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Active").Value!,
            Cpf.Create("98765432100").Value!,
            Email.Create("active@test.com").Value!,
            Phone.Create("11888888888").Value!
        );

        var activeUser = new User(
            UserName.Create("existinguser").Value!,
            "hash",
            Role.Employee,
            activeEmployee.Id
        );

        activeEmployee.AssignUser(activeUser);

        var inactiveEmployee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("Inactive").Value!,
            Cpf.Create("12345678909").Value!,
            Email.Create("inactive@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        inactiveEmployee.Deactive();

        await employeeRepository.AddAsync(activeEmployee);
        await employeeRepository.AddAsync(inactiveEmployee);

        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var input = new RegisterEmployeeInput
        {
            Employee = new EmployeeInput
            {
                Name = "Reactivated",
                Cpf = "12345678909",
                Email = "new@test.com",
                Phone = "11777777777"
            },
            User = new UserInput
            {
                UserName = "existinguser",
                Password = "John@123",
                Role = nameof(Role.Manager)
            }
        };

        var result = await useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Username already registered");
    }
}
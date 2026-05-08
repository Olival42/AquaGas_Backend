using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class DeactiveEmployeeTests : BaseIntegrationTest
{
    private readonly PostgreSqlContainerFixture _fixture;

    private readonly Mock<IUserContextService> _userContextServiceMock = new();
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();

    public DeactiveEmployeeTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    private DeactiveEmployee CreateUseCase(IServiceProvider provider)
    {
        return new DeactiveEmployee(
            provider.GetRequiredService<IEmployeeRepository>(),
            _userContextServiceMock.Object,
            _auditLogServiceMock.Object,
            _refreshTokenRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Deactivate_Employee_Successfully()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        var currentUserId = Guid.NewGuid();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(currentUserId));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByIdAsync(employee.Id, onlyActive: false);

        persisted.Should().NotBeNull();
        persisted!.IsActive.Should().BeFalse();

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                currentUserId,
                "admin",
                AuditAction.DEACTIVATE,
                "Employee",
                employee.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Deactivate_Employee_And_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
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

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByIdAsync(employee.Id, onlyActive: false);

        persisted.Should().NotBeNull();

        persisted!.IsActive.Should().BeFalse();
        persisted.User.Should().NotBeNull();
        persisted.User!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Should_Revoke_Refresh_Tokens_When_Employee_Has_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
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

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(user.Id),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Not_Revoke_Refresh_Tokens_When_Employee_Has_No_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
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

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllByUserId(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Employee not found");
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserId_Context_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                Error.Unauthorized("Unauthorized")));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserName_Context_Fails()
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

        var result = await useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "Unauthorized");
    }

    [Fact]
    public async Task Execute_Should_Return_Conflict_When_User_Tries_To_Deactivate_Himself()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(user.Id));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("john"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().Contain(x =>
            x.Message == "You cannot deactivate your own account");
    }

    [Fact]
    public async Task Execute_Should_Not_Log_Audit_When_Deactivation_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

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
    public async Task Execute_Should_Keep_Same_Employee_Id_After_Deactivation()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var originalId = employee.Id;

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByIdAsync(employee.Id, onlyActive: false);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(originalId);
    }

    [Fact]
    public async Task Execute_Should_Not_Change_Employee_Data_When_Deactivating()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
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

        await useCase.Execute(employee.Id);

        var persisted =
            await employeeRepository.GetByIdAsync(employee.Id, onlyActive: false);

        persisted.Should().NotBeNull();

        persisted!.Name.Value.Should().Be("John Doe");
        persisted.CPF.Value.Should().Be("71447091000");
        persisted.Email.Value.Should().Be("john@test.com");
        persisted.Phone.Value.Should().Be("11999999999");
    }

    [Fact]
    public async Task Execute_Should_Deactivate_Already_Active_User()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        var user = new User(
            UserName.Create("john").Value!,
            "hashed",
            Role.Manager,
            employee.Id
        );

        employee.AssignUser(user);

        await employeeRepository.AddAsync(employee);
        await employeeRepository.SaveChangesAsync();

        user.IsActive.Should().BeTrue();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = CreateUseCase(scope.ServiceProvider);

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();

        var persisted =
            await employeeRepository.GetByIdAsync(employee.Id, onlyActive: false);

        persisted.Should().NotBeNull();
        persisted!.User.Should().NotBeNull();
        persisted.User!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Should_Return_Success_Object()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepository =
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
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

        var result = await useCase.Execute(employee.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_Should_Call_Update_On_Repository()
    {
        using var scope = _fixture.Factory.Services.CreateScope();

        var employeeRepositoryMock = new Mock<IEmployeeRepository>();

        var employee = new AquaGas.Api.Modules.Employee.Domain.Models.Employee(
            EmployeeName.Create("John Doe").Value!,
            Cpf.Create("71447091000").Value!,
            Email.Create("john@test.com").Value!,
            Phone.Create("11999999999").Value!
        );

        employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id))
            .ReturnsAsync(employee);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var useCase = new DeactiveEmployee(
            employeeRepositoryMock.Object,
            _userContextServiceMock.Object,
            _auditLogServiceMock.Object,
            _refreshTokenRepositoryMock.Object
        );

        await useCase.Execute(employee.Id);

        employeeRepositoryMock.Verify(
            x => x.Update(employee),
            Times.Once);
    }
}
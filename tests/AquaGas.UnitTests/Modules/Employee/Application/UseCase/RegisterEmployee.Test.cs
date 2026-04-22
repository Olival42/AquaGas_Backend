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

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
    }

    private RegisterEmployeeInput ValidInput() =>
        new()
        {
            Employee = new()
            {
                Name = "João Silva",
                Cpf = "12345678909",
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
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _useCase.Execute(ValidInput());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Should_Fail_When_Cpf_Exists()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Email_Exists()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Fail_When_Username_Exists()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _useCase.Execute(ValidInput());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_Hash_Password()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        await _useCase.Execute(ValidInput());

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Repositories()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        await _useCase.Execute(ValidInput());

        _employeeRepo.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Once);
        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _employeeRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Log_Audit()
    {
        _employeeRepo.Setup(x => x.AnyByCPFAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _employeeRepo.Setup(x => x.AnyByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepo.Setup(x => x.AnyByUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        await _useCase.Execute(ValidInput());

        _audit.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()
        ), Times.Once);
    }
}
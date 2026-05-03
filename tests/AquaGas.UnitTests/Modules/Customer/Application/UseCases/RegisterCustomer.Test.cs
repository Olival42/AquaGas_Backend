using Xunit;
using Moq;
using FluentAssertions;
using AquaGas.Api.Modules.Customer.Application.UseCases;
using AquaGas.Api.Modules.Customer.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Modules.Customer.Domain.Models;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.API.Modules.Customer.Application.Mappings;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Shared.Domain.ValueObjects;

public class RegisterCustomerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly Mock<IUserContextService> _userContextMock;

    private readonly RegisterCustomer _useCase;

    public RegisterCustomerTests()
    {
        CustomerMapping.Register();

        _repositoryMock = new Mock<ICustomerRepository>();
        _auditMock = new Mock<IAuditLogService>();
        _userContextMock = new Mock<IUserContextService>();

        _useCase = new RegisterCustomer(
            _repositoryMock.Object,
            _auditMock.Object,
            _userContextMock.Object
        );
    }

    private RegisterCustomerInput CreateValidInput()
    {
        return new RegisterCustomerInput
        {
            Name = "João",
            Document = "55964416004",
            Email = "email@email.com",
            Phone = "44999999999",
            Address = new RegisterAddressInput
            {
                Street = "Rua A",
                Number = "123",
                Neighborhood = "Centro",
                City = "Maringá",
                Cep = "87000000"
            }
        };
    }

    private static Customer CreateInactiveCustomerMatching(RegisterCustomerInput input)
    {
        var name = CustomerName.Create("Nome Antigo").Value!;
        var document = Document.Create(input.Document).Value!;
        var email = Email.Create("velho@email.com").Value!;
        var phone = Phone.Create("44000000000").Value!;

        var customer = new Customer(name, document, email, phone);
        customer.Deactive();
        return customer;
    }

    [Fact]
    public async Task Should_Register_Customer_Successfully()
    {
        var input = CreateValidInput();

        _repositoryMock.Setup(x => x.GetByDocumentAsync(input.Document))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Return_Error_When_Document_Already_Exists_Active()
    {
        var input = CreateValidInput();

        var active = new Customer(
            CustomerName.Create(input.Name).Value!,
            Document.Create(input.Document).Value!,
            Email.Create(input.Email).Value!,
            Phone.Create(input.Phone).Value!);

        _repositoryMock.Setup(x => x.GetByDocumentAsync(input.Document))
            .ReturnsAsync(active);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Return_Error_When_UserId_Fails()
    {
        var input = CreateValidInput();

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Error_When_UserName_Fails()
    {
        var input = CreateValidInput();

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail());

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Error_When_Validation_Fails()
    {
        var input = new RegisterCustomerInput
        {
            Name = ""
        };

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Call_Repository_When_Validation_Fails()
    {
        var input = new RegisterCustomerInput
        {
            Name = ""
        };

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Save_When_Document_Already_Exists_Active()
    {
        var input = CreateValidInput();

        var active = new Customer(
            CustomerName.Create(input.Name).Value!,
            Document.Create(input.Document).Value!,
            Email.Create(input.Email).Value!,
            Phone.Create(input.Phone).Value!);

        _repositoryMock.Setup(x => x.GetByDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(active);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Lookup_Document_Using_Correct_Value()
    {
        var input = CreateValidInput();

        _repositoryMock.Setup(x => x.GetByDocumentAsync(input.Document))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(input);

        _repositoryMock.Verify(x => x.GetByDocumentAsync(input.Document), Times.Once);
    }

    [Fact]
    public async Task Should_Add_Address_To_Customer()
    {
        var input = CreateValidInput();

        _repositoryMock.Setup(x => x.GetByDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        Customer? capturedCustomer = null;

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .Callback<Customer>(c => capturedCustomer = c)
            .Returns(Task.CompletedTask);

        await _useCase.Execute(input);

        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_Return_Mapped_Dto_Correctly()
    {
        var input = CreateValidInput();

        _repositoryMock.Setup(x => x.GetByDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Name.Should().Be(input.Name);
        result.Value.Document.Should().Be(input.Document);
        result.Value.Email.Should().Be(input.Email);
    }

    [Fact]
    public async Task Should_Call_Audit_Log_With_Correct_Action_On_Create()
    {
        var input = CreateValidInput();

        var userId = Guid.NewGuid();

        _repositoryMock.Setup(x => x.GetByDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(input);

        _auditMock.Verify(x => x.LogAsync(
            userId,
            It.IsAny<string?>(),
            AuditAction.CREATE,
            "Customer",
            It.IsAny<Guid>(),
            null,
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Reactivate_Inactive_Customer_And_Update_Data()
    {
        var input = CreateValidInput();
        var inactive = CreateInactiveCustomerMatching(input);

        _repositoryMock.Setup(x => x.GetByDocumentAsync(input.Document))
            .ReturnsAsync(inactive);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Name.Should().Be(input.Name);
        result.Value.Email.Should().Be(input.Email);

        inactive.IsActive.Should().BeTrue();
        inactive.Addresses.Should().HaveCount(1);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            AuditAction.UPDATE,
            "Customer",
            inactive.Id,
            It.IsAny<object?>(),
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Audit_Log_When_Fails_Active_Duplicate()
    {
        var input = CreateValidInput();

        var active = new Customer(
            CustomerName.Create(input.Name).Value!,
            Document.Create(input.Document).Value!,
            Email.Create(input.Email).Value!,
            Phone.Create(input.Phone).Value!);

        _repositoryMock.Setup(x => x.GetByDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(active);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(input);

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<object?>(),
            It.IsAny<object>()), Times.Never);
    }
}

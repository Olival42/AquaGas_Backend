using Xunit;
using Moq;
using FluentAssertions;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Auth.Application.Services;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Domain.Models;
using AquaGas.Shared.Results;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Customer.Domain.ValueObjects.Address;
using AquaGas.Application.Services;
using Mapster;

public class UpdateCustomerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly UpdateCustomer _useCase;

    public UpdateCustomerTests()
{
    _repositoryMock = new Mock<ICustomerRepository>();
    _auditMock = new Mock<IAuditLogService>();
    _userContextMock = new Mock<IUserContextService>();

    _useCase = new UpdateCustomer(
        _repositoryMock.Object,
        _auditMock.Object,
        _userContextMock.Object
    );
}

    private void SetupValidUser()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));
    }

    private Customer CreateCustomer()
    {
        var customer = new Customer(
            CustomerName.Create("João").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("email@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        return customer;
    }

    private UpdateCustomerInput CreateValidInput()
    {
        return new UpdateCustomerInput
        {
            Name = "Novo Nome"
        };
    }

    [Fact]
    public async Task Should_Update_Customer_Successfully()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        SetupValidUser();

        var input = CreateValidInput();

        var result = await _useCase.Execute(input, customer.Id);

        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(x => x.Update(customer), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            "admin",
            AuditAction.UPDATE,
            "Customer",
            customer.Id,
            It.IsAny<object>(),
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Should_Return_Error_When_Customer_Not_Found()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Customer?)null);

        SetupValidUser();

        var result = await _useCase.Execute(CreateValidInput(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Should_Return_Error_When_UserId_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        var result = await _useCase.Execute(CreateValidInput(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Error_When_Document_Already_Exists()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _repositoryMock.Setup(r => r.AnyByDocumentAsync("55964416004", customer.Id))
            .ReturnsAsync(true);

        SetupValidUser();

        var input = new UpdateCustomerInput
        {
            Document = "12345678900"
        };

        var result = await _useCase.Execute(input, customer.Id);

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Error_When_Address_Not_Found()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        SetupValidUser();

        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = Guid.NewGuid(),
                Street = "Rua A",
                Neighborhood = "Centro",
                Number = "123",
                City = "Maringá",
                Cep = "87000000"
            }
        };

        var result = await _useCase.Execute(input, customer.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Error_When_Validation_Fails()
    {
        var input = new UpdateCustomerInput
        {
            Name = ""
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Update_When_Validation_Fails()
    {
        var input = new UpdateCustomerInput
        {
            Name = ""
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Call_Repository_When_UserId_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        var result = await _useCase.Execute(CreateValidInput(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Call_Repository_When_UserName_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail());

        var result = await _useCase.Execute(CreateValidInput(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Save_When_Document_Already_Exists()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _repositoryMock.Setup(r => r.AnyByDocumentAsync("55964416004", customer.Id))
            .ReturnsAsync(true);

        SetupValidUser();

        var input = new UpdateCustomerInput
        {
            Document = "12345678900"
        };

        var result = await _useCase.Execute(input, customer.Id);

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Update_Only_Name_When_Only_Name_Is_Provided()
    {
        var customer = CreateCustomer();

        var originalEmail = customer.Email.Value;

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        SetupValidUser();

        var input = new UpdateCustomerInput
        {
            Name = "Novo Nome"
        };

        await _useCase.Execute(input, customer.Id);

        customer.Name.Value.Should().Be("Novo Nome");
        customer.Email.Value.Should().Be(originalEmail);
    }

    [Fact]
    public async Task Should_Update_Address_When_Provided()
    {
        var customer = CreateCustomer();

        var address = new Address(
            Guid.NewGuid(),
            "Rua A",
            "Centro",
            "123",
            null,
            "Maringá",
            Cep.Create("87000000").Value!
        );

        customer.AddAddress(address);

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        SetupValidUser();

        var input = new UpdateCustomerInput
        {
            Address = new UpdateAddressInput
            {
                AddressId = address.Id,
                Street = "Rua Nova",
                Neighborhood = "Centro",
                Number = "999",
                City = "Maringá",
                Cep = "87000000"
            }
        };

        await _useCase.Execute(input, customer.Id);

        address.Street.Should().Be("Rua Nova");
        address.Number.Should().Be("999");
    }

    [Fact]
    public async Task Should_Call_Audit_Log_With_Update_Action()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(r => r.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        var userId = Guid.NewGuid();

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(CreateValidInput(), customer.Id);

        _auditMock.Verify(x => x.LogAsync(
            userId,
            "admin",
            AuditAction.UPDATE,
            "Customer",
            customer.Id,
            It.Is<object>(o => o != null),
            It.Is<object>(o => o != null)
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Audit_When_Fails()
    {
        var result = await _useCase.Execute(
            new UpdateCustomerInput { Name = "" },
            Guid.NewGuid()
        );

        result.IsFailure.Should().BeTrue();

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<object>(),
            It.IsAny<object>()), Times.Never);
    }
}
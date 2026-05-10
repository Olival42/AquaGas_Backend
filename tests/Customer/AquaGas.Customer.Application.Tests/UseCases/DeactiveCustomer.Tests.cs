using Xunit;
using Moq;
using FluentAssertions;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Auth.Application.Services;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Customer.Application.Mappings;
using AquaGas.Application.Services;

public class DeactiveCustomerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly Mock<IAuditLogService> _auditMock;

    private readonly DeactiveCustomer _useCase;

    public DeactiveCustomerTests()
    {
        CustomerMapping.Register();

        _repositoryMock = new Mock<ICustomerRepository>();
        _userContextMock = new Mock<IUserContextService>();
        _auditMock = new Mock<IAuditLogService>();

        _useCase = new DeactiveCustomer(
            _repositoryMock.Object,
            _userContextMock.Object,
            _auditMock.Object
        );
    }

    private Customer CreateCustomer()
    {
        return new Customer(
            CustomerName.Create("João").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("email@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    [Fact]
    public async Task Should_Deactive_Customer_Successfully()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(customer.Id);

        result.IsSuccess.Should().BeTrue();
        customer.IsActive.Should().BeFalse();

        _repositoryMock.Verify(x => x.Update(customer), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);

        _auditMock.Verify(x => x.LogAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            AuditAction.DEACTIVATE,
            "Customer",
            customer.Id,
            It.IsAny<object>(),
            It.IsAny<object>()),
        Times.Once);
    }

    [Fact]
    public async Task Should_Return_Error_When_UserId_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        var result = await _useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Should_Return_Error_When_UserName_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail());

        var result = await _useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();

        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Should_Return_Error_When_Customer_Not_Found()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        var result = await _useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == "Customer not found");

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Save_When_Customer_Not_Found()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Customer?)null);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(Guid.NewGuid());

        _repositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Call_Audit_With_Correct_Data()
    {
        var customer = CreateCustomer();
        var userId = Guid.NewGuid();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(customer.Id);

        _auditMock.Verify(x => x.LogAsync(
            userId,
            "admin",
            AuditAction.DEACTIVATE,
            "Customer",
            customer.Id,
            It.IsAny<object>(),
            It.IsAny<object>()),
        Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Audit_When_Fails()
    {
        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        await _useCase.Execute(Guid.NewGuid());

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
    public async Task Should_Change_IsActive_From_True_To_False()
    {
        var customer = CreateCustomer();
        customer.IsActive.Should().BeTrue();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        await _useCase.Execute(customer.Id);

        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Not_Change_State_When_Fails()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail());

        var originalState = customer.IsActive;

        await _useCase.Execute(customer.Id);

        customer.IsActive.Should().Be(originalState);
    }

    [Fact]
    public async Task Should_Throw_When_Repository_Fails()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("db error"));

        _userContextMock.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        Func<Task> act = async () => await _useCase.Execute(Guid.NewGuid());

        await act.Should().ThrowAsync<Exception>();
    }
}
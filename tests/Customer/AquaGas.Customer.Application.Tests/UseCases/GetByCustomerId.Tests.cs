using Xunit;
using Moq;
using FluentAssertions;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Application.Mappings;

public class GetByCustomerIdTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly GetByCustomerId _useCase;

    public GetByCustomerIdTests()
    {
        CustomerMapping.Register();

        _repositoryMock = new Mock<ICustomerRepository>();
        _useCase = new GetByCustomerId(_repositoryMock.Object);
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

    [Fact]
    public async Task Should_Return_Customer_When_Found()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        var result = await _useCase.Execute(customer.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(customer.Name.Value);
    }

    [Fact]
    public async Task Should_Return_Error_When_Customer_Not_Found()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == "Customer not found");
    }

    [Fact]
    public async Task Should_Call_Repository_With_Correct_Id()
    {
        var id = Guid.NewGuid();

        _repositoryMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((Customer?)null);

        await _useCase.Execute(id);

        _repositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task Should_Map_All_Properties_Correctly()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        var result = await _useCase.Execute(customer.Id);

        var dto = result.Value!;

        dto.Name.Should().Be(customer.Name.Value);
        dto.Email.Should().Be(customer.Email.Value);
        dto.Phone.Should().Be(customer.Phone.Value);
        dto.Document.Should().Be(customer.Document.Value);
    }

    [Fact]
    public async Task Should_Not_Return_Null_When_Success()
    {
        var customer = CreateCustomer();

        _repositoryMock.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        var result = await _useCase.Execute(customer.Id);

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Handle_Empty_Guid()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(Guid.Empty))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.Execute(Guid.Empty);

        result.IsFailure.Should().BeTrue();
    }
}
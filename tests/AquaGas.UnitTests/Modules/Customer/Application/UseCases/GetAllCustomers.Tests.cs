using Xunit;
using Moq;
using FluentAssertions;
using AquaGas.Api.Modules.Customer.Application.UseCases;
using AquaGas.Api.Modules.Customer.Domain.Repositories;
using AquaGas.Api.Modules.Customer.Domain.Models;
using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.API.Modules.Customer.Application.Mappings;

public class GetAllCustomersTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly GetAllCustomers _useCase;

    public GetAllCustomersTests()
    {
        CustomerMapping.Register();

        _repositoryMock = new Mock<ICustomerRepository>();
        _useCase = new GetAllCustomers(_repositoryMock.Object);
    }

    private Customer CreateCustomer(string name, string document)
    {
        var customer = new Customer(
            CustomerName.Create(name).Value!,
            Document.Create(document).Value!,
            Email.Create("email@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        return customer;
    }

    [Fact]
    public async Task Should_Return_Only_Active_Customers()
    {
        var activeCustomer = CreateCustomer("João", "55964416004");
        var inactiveCustomer = CreateCustomer("Maria", "14584553009");

        inactiveCustomer.Deactive();

        var customers = new List<Customer>
        {
            activeCustomer,
            inactiveCustomer
        };

        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(customers);

        var result = await _useCase.Execute();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("João");
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Active_Customers()
    {
        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer>());

        var result = await _useCase.Execute();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Call_Repository_Once()
    {
        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer>());

        await _useCase.Execute();

        _repositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Return_Null_List()
    {
        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer>());

        var result = await _useCase.Execute();

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Map_All_Active_Customers()
    {
        var c1 = CreateCustomer("João", "14584553009");
        var c2 = CreateCustomer("Maria", "60755639030");

        c2.Deactive();

        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer> { c1, c2 });

        var result = await _useCase.Execute();

        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_Map_Customer_Properties_Correctly()
    {
        var customer = CreateCustomer("João", "60416772056");

        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer> { customer });

        var result = await _useCase.Execute();

        var dto = result.Value!.First();

        dto.Name.Should().Be(customer.Name.Value);
        dto.Email.Should().Be(customer.Email.Value);
    }

    [Fact]
    public async Task Should_Throw_When_Repository_Throws()
    {
        _repositoryMock.Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("db error"));

        Func<Task> act = async () => await _useCase.Execute();

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("db error");
    }
}
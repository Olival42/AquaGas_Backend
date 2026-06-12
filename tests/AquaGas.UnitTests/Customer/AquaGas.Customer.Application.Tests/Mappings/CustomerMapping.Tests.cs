using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Address;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Customer.Application.Mappings;
using FluentAssertions;
using Mapster;
using Xunit;
using AquaGas.Shared.Domain.ValueObjects;

public class CustomerMappingTests
{
    private readonly TypeAdapterConfig _config;

    public CustomerMappingTests()
    {
        _config = new TypeAdapterConfig();
        CustomerMapping.Register(_config);
    }

    private Customer CreateCustomerWithAddress()
    {
        var customer = new Customer(
            CustomerName.Create("João").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("email@email.com").Value!,
            Phone.Create("44999999999").Value!
        );

        customer.AddAddress(CreateAddress("Rua A"));

        return customer;
    }

    private Customer CreateCustomerWithoutAddress()
    {
        return new Customer(
            CustomerName.Create("João").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("email@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }

    private Address CreateAddress(string street)
    {
        return new Address(
            Guid.NewGuid(),
            street,
            "Centro",
            "123",
            null,
            "Maringá",
            Cep.Create("87000000").Value!
        );
    }

    [Fact]
    public void Should_Map_Customer_To_Response_Correctly()
    {
        var customer = CreateCustomerWithAddress();

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Id.Should().Be(customer.Id);
        result.Name.Should().Be(customer.Name.Value);
        result.Document.Should().Be(customer.Document.Value);
        result.TypeDocument.Should().Be(customer.Document.Type.ToString());
        result.Email.Should().Be(customer.Email.Value);
        result.Phone.Should().Be(customer.Phone.Value);

        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("Rua A");
    }

    [Fact]
    public void Should_Map_Document_As_String_Not_Object()
    {
        var customer = CreateCustomerWithAddress();

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Document.Should().BeOfType<string>();
        result.Document.Should().Be("55964416004");
    }

    [Fact]
    public void Should_Map_Email_And_Phone_Correctly()
    {
        var customer = CreateCustomerWithAddress();

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Email.Should().Be("email@email.com");
        result.Phone.Should().Be("44999999999");
    }

    [Fact]
    public void Should_Map_Address_Correctly()
    {
        var customer = CreateCustomerWithAddress();

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("Rua A");
        result.Address.Neighborhood.Should().Be("Centro");
        result.Address.Number.Should().Be("123");
        result.Address.City.Should().Be("Maringá");
        result.Address.Cep.Should().Be("87000000");
    }

    [Fact]
    public void Should_Return_Null_Address_When_No_Address()
    {
        var customer = CreateCustomerWithoutAddress();

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Address.Should().BeNull();
    }

    [Fact]
    public void Should_Map_Only_First_Address()
    {
        var customer = CreateCustomerWithoutAddress();

        customer.AddAddress(CreateAddress("Rua A"));
        customer.AddAddress(CreateAddress("Rua B"));

        var result = customer.Adapt<CustomerResponse>(_config);

        result.Address!.Street.Should().Be("Rua A");
    }

    [Fact]
    public void Should_Map_RegisterCustomerValidated_To_Customer()
    {
        var validated = new RegisterCustomerValidated
        {
            Name = CustomerName.Create("João").Value!,
            Document = Document.Create("55964416004").Value!,
            Email = Email.Create("email@email.com").Value!,
            Phone = Phone.Create("44999999999").Value!,
            Address = new RegisterAddressValidated
            {
                Street = "Rua A",
                Neighborhood = "Centro",
                Number = "123",
                City = "Maringá",
                Cep = Cep.Create("87000000").Value!
            }
        };

        var customer = validated.Adapt<Customer>(_config);

        customer.Name.Value.Should().Be("João");
        customer.Document.Value.Should().Be("55964416004");
        customer.Email.Value.Should().Be("email@email.com");
        customer.Phone.Value.Should().Be("44999999999");

        customer.Addresses.Should().BeEmpty();
    }

    [Fact]
    public void Should_Map_RegisterAddressValidated_To_Address()
    {
        var validated = new RegisterAddressValidated
        {
            Street = "Rua A",
            Neighborhood = "Centro",
            Number = "123",
            Complement = "Casa",
            City = "Maringá",
            Cep = Cep.Create("87000000").Value!
        };

        var address = validated.Adapt<Address>(_config);

        address.Street.Should().Be("Rua A");
        address.Neighborhood.Should().Be("Centro");
        address.Number.Should().Be("123");
        address.Complement.Should().Be("Casa");
        address.City.Should().Be("Maringá");
        address.Cep.Value.Should().Be("87000000");
    }

    [Fact]
    public void Should_Map_Address_To_Response_Correctly()
    {
        var address = CreateAddress("Rua A");

        var result = address.Adapt<AddressResponse>(_config);

        result.Id.Should().Be(address.Id);
        result.Street.Should().Be(address.Street);
        result.Neighborhood.Should().Be(address.Neighborhood);
        result.Number.Should().Be(address.Number);
        result.City.Should().Be(address.City);
        result.Cep.Should().Be(address.Cep.Value);
    }
}

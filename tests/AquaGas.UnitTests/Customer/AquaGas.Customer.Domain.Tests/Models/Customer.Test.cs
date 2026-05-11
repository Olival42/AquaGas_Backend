using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Address;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class CustomerTests
{
    [Fact]
    public void Should_Create_Customer_Successfully()
    {
        var name = CustomerName.Create("João Silva").Value!;
        var document = Document.Create("12345678909").Value!;
        var email = Email.Create("joao@email.com").Value!;
        var phone = Phone.Create("44999999999").Value!;

        var customer = new Customer(name, document, email, phone);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.True(customer.IsActive);
        Assert.Equal(name, customer.Name);
        Assert.Equal(document, customer.Document);
        Assert.Equal(email, customer.Email);
        Assert.Equal(phone, customer.Phone);
    }

    [Fact]
    public void Should_Deactivate_Customer()
    {
        var customer = CreateCustomer();

        customer.Deactive();

        Assert.False(customer.IsActive);
    }

    [Fact]
    public void Should_Reactivate_Customer()
    {
        var customer = CreateCustomer();

        customer.Deactive();
        customer.Reactivate();

        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Should_Update_Only_Provided_Fields()
    {
        var customer = CreateCustomer();

        var oldEmail = customer.Email;
        var newName = CustomerName.Create("Novo Nome").Value!;

        customer.Update(newName, null, null, null);

        customer.Name.Should().Be(newName);
        customer.Email.Should().Be(oldEmail);
    }

    [Fact]
    public void Should_Not_Update_When_Values_Are_Null()
    {
        var customer = CreateCustomer();

        var oldName = customer.Name;

        customer.Update(null, null, null, null);

        Assert.Equal(oldName, customer.Name);
    }

    [Fact]
    public void Should_Add_Address_To_Customer()
    {
        var customer = CreateCustomer();
        var cep = Cep.Create("87000000").Value!;

        var address = new Address(
            customer.Id,
            "Rua A",
            "Centro",
            "123",
            null,
            "Maringá",
            cep
        );

        customer.AddAddress(address);

        customer.Addresses.Should().HaveCount(1);
        customer.Addresses.First().Street.Should().Be("Rua A");
    }

    [Fact]
public void Should_Replace_Existing_Address()
{
    var customer = CreateCustomer();

    var oldAddress = new Address(
        customer.Id,
        "Rua A",
        "Centro",
        "123",
        null,
        "Maringá",
        Cep.Create("87000000").Value!
    );

    customer.AddAddress(oldAddress);

    var newAddress = new Address(
        customer.Id,
        "Rua Nova",
        "Bairro X",
        "999",
        null,
        "Maringá",
        Cep.Create("87000000").Value!
    );

    customer.ReplaceAddresses(newAddress);

    customer.Addresses.Should().HaveCount(1);
    customer.Addresses.First().Street.Should().Be("Rua Nova");
}

    private Customer CreateCustomer()
    {
        return new Customer(
            CustomerName.Create("João Silva").Value!,
            Document.Create("12788914040").Value!,
            Email.Create("joao@email.com").Value!,
            Phone.Create("44999999999").Value!
        );
    }
}
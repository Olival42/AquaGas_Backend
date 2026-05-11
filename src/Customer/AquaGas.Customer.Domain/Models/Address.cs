using AquaGas.Customer.Domain.ValueObjects.Address;

namespace AquaGas.Customer.Domain.Models;

public class Address
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public string Street { get; private set; } = null!;
    public string Neighborhood { get; private set; } = null!;
    public string Number { get; private set; } = null!;
    public string? Complement { get; private set; }
    public string City { get; private set; } = null!;
    public Cep Cep { get; private set; } = null!;

    public Customer Customer { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    private Address() { }

    public Address(
        Guid customerId,
        string street,
        string neighborhood,
        string number,
        string? complement,
        string city,
        Cep cep)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Street = street;
        Neighborhood = neighborhood;
        Number = number;
        Complement = complement;
        City = city;
        Cep = cep;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? street,
        string? neighborhood,
        string? number,
        string? complement,
        string? city,
        Cep? cep)
    {
        if (street is not null)
            Street = street;

        if (neighborhood is not null)
            Neighborhood = neighborhood;

        if (number is not null)
            Number = number;

        if (complement is not null)
            Complement = complement;

        if (city is not null)
            City = city;

        if (cep is not null)
            Cep = cep;
    }
}
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Customer.Domain.Models;

public class Customer
{
    public Guid Id { get; private set; }

    public CustomerName Name { get; private set; } = null!;
    public Document Document { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;

    public List<Address> Addresses { get; private set; } = new();

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Customer() { }

    public Customer(
        CustomerName name,
        Document document,
        Email email,
        Phone phone)
    {
        Id = Guid.NewGuid();
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactive() => IsActive = false;

    public void Reactivate() => IsActive = true;

    public void AddAddress(Address address)
    {
        Addresses.Add(address);
    }

    public void Update(
        CustomerName? name,
        Document? document,
        Email? email,
        Phone? phone)
    {
        if (name is not null)
            Name = name;

        if (document is not null)
            Document = document;

        if (email is not null)
            Email = email;

        if (phone is not null)
            Phone = phone;
    }

    public void ReplaceAddresses(Address newAddress)
    {
        var existing = Addresses.FirstOrDefault();

        if (existing != null)
        {
            existing.Update(
                newAddress.Street,
                newAddress.Neighborhood,
                newAddress.Number,
                newAddress.Complement,
                newAddress.City,
                newAddress.Cep
            );
        }
        else
        {
            AddAddress(newAddress);
        }
    }
}
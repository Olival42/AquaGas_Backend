using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;

namespace AquaGas.Api.Modules.Employee.Domain.Models;

public class Employee
{
    public Guid Id { get; private set; }

    public EmployeeName Name { get; private set; } = null!;
    public Cpf CPF { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;

    public User? User { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Employee() { }

    public Employee(
        EmployeeName name,
        Cpf cpf,
        Email email,
        Phone phone)
    {
        Id = Guid.NewGuid();
        Name = name;
        CPF = cpf;
        Email = email;
        Phone = phone;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactive()
    {
        IsActive = false;
        User?.Deactive();
    }

    public void Update(
    EmployeeName? name,
    Email? email,
    Phone? phone)
    {
        if (name is not null)
            Name = name;

        if (email is not null)
            Email = email;

        if (phone is not null)
            Phone = phone;
    }

}
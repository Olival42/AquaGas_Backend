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
}
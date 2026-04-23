namespace AquaGas.Api.Modules.Auth.Domain.Models;

using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Domain.Models;

public class User
{
    public Guid Id { get; private set; }

    public UserName UserName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Role Role { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    private User() { }

    public User(UserName userName, string passwordHash, Role role, Guid employeeId)
    {
        Id = Guid.NewGuid();
        UserName = userName;
        PasswordHash = passwordHash;
        Role = role;
        EmployeeId = employeeId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactive() => IsActive = false;

    public void Active() => IsActive = true;

    public bool ChangeUserName(UserName? newUserName)
    {
        if (newUserName is null || UserName == newUserName)
            return false;

        UserName = newUserName;
        return true;
    }

    public bool ChangeRole(Role? newRole)
    {
        if (newRole is null || Role == newRole.Value)
            return false;

        Role = newRole.Value;
        return true;
    }

    public bool ChangePassword(Password? password, IPasswordHasher hasher)
    {
        if (password is null)
            return false;

        var newHash = hasher.Hash(password.Value);

        if (PasswordHash == newHash)
            return false;

        PasswordHash = newHash;
        return true;
    }
}
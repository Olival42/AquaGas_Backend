namespace AquaGas.Auth.Domain.Factories;

using AquaGas.Auth.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public static class RoleFactory
{
    public static Result<Role> Create(string role)
    {
        if (!Enum.TryParse<Role>(role, true, out var parsed))
            return Result<Role>.Fail(
                Error.Validation("Role is invalid. Allowed: Manager, Employee", "Role")
            );

        if (!Enum.IsDefined(typeof(Role), parsed))
            return Result<Role>.Fail(
                Error.Validation("Role is invalid. Allowed: Manager, Employee", "Role")
            );

        return Result<Role>.Success(parsed);
    }
}
namespace AquaGas.Api.Modules.Auth.Domain.Factories;

using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

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
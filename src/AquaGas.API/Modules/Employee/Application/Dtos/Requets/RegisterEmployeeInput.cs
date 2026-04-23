using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;

namespace AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public record RegisterEmployeeInput
{
    public UserInput User {get; init;} = null!;
    public EmployeeInput Employee {get; init;} = null!;
}
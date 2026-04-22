namespace AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public record UpdateEmployeeInput
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Role {get; init; }
}
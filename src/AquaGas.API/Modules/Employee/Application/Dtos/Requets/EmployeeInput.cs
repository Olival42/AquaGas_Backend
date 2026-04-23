namespace AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

public record EmployeeInput
{
    public string Name { get; init; } = null!;
    public string Cpf { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Phone { get; init; } = null!;
}
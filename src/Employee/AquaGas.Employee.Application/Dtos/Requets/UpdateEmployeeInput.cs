namespace AquaGas.Employee.Application.Dtos.Requests;

public record UpdateEmployeeInput
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? UserName { get; init; }
    public string? NewPassword { get; set; }
    public string? Role { get; init; }
}
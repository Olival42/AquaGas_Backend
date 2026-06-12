namespace AquaGas.Employee.Application.Dtos.Responses;

public record EmployeeResponse(
    Guid Id,
    string Name,
    string Cpf,
    string Email,
    string Phone,
    DateTime CreatedAt)
{ }
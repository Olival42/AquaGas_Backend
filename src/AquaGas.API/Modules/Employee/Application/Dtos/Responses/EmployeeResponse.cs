namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record EmployeeResponse(
    Guid Id,
    string Name,
    string Cpf,
    string Email,
    string Phone)
{ }
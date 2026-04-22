namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;

public record RegisterEmployeeValidated(
    EmployeeName Name,
    Cpf Cpf,
    Email Email,
    Phone Phone,
    UserName UserName,
    Password Password,
    Role Role
);
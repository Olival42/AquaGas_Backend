namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;

public record UpdateEmployeeValidated(
    EmployeeName? Name,
    Email? Email,
    Phone? Phone,
    Role? Role
);
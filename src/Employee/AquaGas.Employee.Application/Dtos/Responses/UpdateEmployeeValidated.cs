namespace AquaGas.Auth.Application.Dtos.Responses;

using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;

public record UpdateEmployeeValidated(
    EmployeeName? Name,
    Email? Email,
    Phone? Phone,
    UserName? UserName,
    Password? Password,
    Role? Role
);
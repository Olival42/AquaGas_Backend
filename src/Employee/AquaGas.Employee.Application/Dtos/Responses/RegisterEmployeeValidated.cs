using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Employee.Application.Dtos.Responses;

public record RegisterEmployeeValidated(
    EmployeeName Name,
    Cpf Cpf,
    Email Email,
    Phone Phone,
    UserName UserName,
    Password Password,
    Role Role
);
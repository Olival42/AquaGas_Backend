using AquaGas.Auth.Application.Dtos.Responses;

namespace AquaGas.Employee.Application.Dtos.Responses;

public record EmployeeWithUserResponse(
    UserResponse User,
    EmployeeResponse Employee)
{ }
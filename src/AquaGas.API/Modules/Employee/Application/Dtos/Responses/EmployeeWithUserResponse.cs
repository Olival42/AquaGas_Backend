using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record EmployeeWithUserResponse(
    UserResponse User,
    EmployeeResponse Employee)
{ }
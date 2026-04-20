using AquaGas.Api.Modules.Auth.Domain.Enums;

namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record UserResponse(
    Guid UserId,
    string UserName,
    Role Role,
    Guid EmployeeId)
{ }
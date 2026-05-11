using AquaGas.Auth.Domain.Enums;

namespace AquaGas.Auth.Application.Dtos.Responses;

public record UserResponse(
    Guid UserId,
    string UserName,
    Role Role)
{ }
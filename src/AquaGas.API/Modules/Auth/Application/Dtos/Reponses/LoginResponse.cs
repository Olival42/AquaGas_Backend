namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record LoginResponse(
    UserResponse User,
    string AccessToken,
    long ExpiresAt)
{ }
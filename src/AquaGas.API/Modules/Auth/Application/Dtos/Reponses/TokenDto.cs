namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record TokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    long AccessExpiresAt)
{ }
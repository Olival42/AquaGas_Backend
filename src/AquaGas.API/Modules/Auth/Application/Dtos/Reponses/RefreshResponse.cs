namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record RefreshResponse(
    string AccessToken,
    long ExpiresAt)
{ }
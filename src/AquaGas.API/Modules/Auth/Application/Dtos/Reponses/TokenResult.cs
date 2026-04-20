namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record TokenResult(
    string Token,
    DateTime ExpiresAt
);
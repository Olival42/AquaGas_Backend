namespace AquaGas.Auth.Application.Dtos.Responses;

public record TokenResult(
    string Token,
    DateTime ExpiresAt
);
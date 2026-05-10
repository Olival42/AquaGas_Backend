namespace AquaGas.Auth.Application.Dtos.Responses;

public record LoginResult(
    UserResponse User,
    TokenDto Tokens)
{ }
namespace AquaGas.Api.Modules.Auth.Application.Dtos.Responses;

public record LoginResult(
    UserResponse User,
    TokenDto Tokens)
{ }
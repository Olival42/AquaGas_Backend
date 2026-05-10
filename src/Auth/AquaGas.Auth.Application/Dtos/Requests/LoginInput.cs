namespace AquaGas.Auth.Application.Dtos.Requests;

public record LoginInput
{
        public string UserName { get; init; } = null!;
        public string Password { get; init; } = null!;
}
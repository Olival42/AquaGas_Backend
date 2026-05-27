using AquaGas.Shared.OpenApi;

namespace AquaGas.Auth.Application.Dtos.Requests;

/// <summary>
/// Credenciais para autenticação na API.
/// </summary>
public record LoginInput
{
    /// <summary>Nome de usuário cadastrado.</summary>
    [OpenApiField(
        Description = "Identificador de login (3 a 50 caracteres alfanuméricos).",
        Example = "gerente",
        Pattern = @"^[a-zA-Z0-9]{3,50}$",
        MinLength = 3,
        MaxLength = 50)]
    public string UserName { get; init; } = null!;

    /// <summary>Senha do usuário.</summary>
    [OpenApiField(
        Description = "Mínimo 8 caracteres: maiúscula, minúscula, número e especial (@$!%*?&).",
        Example = "Senha@123",
        MinLength = 8)]
    public string Password { get; init; } = null!;
}

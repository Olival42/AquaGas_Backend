using AquaGas.Shared.OpenApi;

namespace AquaGas.Auth.Application.Dtos.Requests;

/// <summary>
/// Dados de usuário para cadastro ou atualização de acesso.
/// </summary>
public record UserInput
{
    /// <summary>Nome de usuário.</summary>
    [OpenApiField(
        Example = "joao.vendas",
        Pattern = @"^[a-zA-Z0-9]{3,50}$",
        MinLength = 3,
        MaxLength = 50)]
    public string UserName { get; init; } = null!;

    /// <summary>Senha em texto plano (será armazenada com hash).</summary>
    [OpenApiField(Example = "Senha@123", MinLength = 8)]
    public string Password { get; init; } = null!;

    /// <summary>Perfil de acesso.</summary>
    [OpenApiField(
        Example = "Employee",
        AllowedValues = ["Manager", "Employee"])]
    public string Role { get; init; } = null!;
}

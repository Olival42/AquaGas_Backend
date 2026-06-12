using AquaGas.Shared.OpenApi;

namespace AquaGas.Customer.Application.Dtos.Requests;

/// <summary>
/// Endereço para cadastro de cliente.
/// </summary>
public record RegisterAddressInput
{
    /// <summary>Logradouro (rua, avenida, etc.).</summary>
    [OpenApiField(Example = "Rua das Flores", MinLength = 1)]
    public string Street { get; init; } = null!;

    /// <summary>Bairro.</summary>
    [OpenApiField(Example = "Centro", MinLength = 1)]
    public string Neighborhood { get; init;} = null!;

    /// <summary>Número.</summary>
    [OpenApiField(Example = "100")]
    public string Number { get; init; } = null!;

    /// <summary>Complemento (opcional).</summary>
    [OpenApiField(Example = "Apto 12")]
    public string? Complement { get; init; }

    /// <summary>Cidade.</summary>
    [OpenApiField(Example = "São Paulo", MinLength = 1)]
    public string City { get; init; } = null!;

    /// <summary>CEP brasileiro (8 dígitos).</summary>
    [OpenApiField(
        Example = "01001000",
        Pattern = @"^\d{8}$",
        MinLength = 8,
        MaxLength = 8)]
    public string Cep { get; init; } = null!;
}

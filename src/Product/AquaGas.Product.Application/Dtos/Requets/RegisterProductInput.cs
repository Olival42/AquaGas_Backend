using AquaGas.Shared.OpenApi;

namespace AquaGas.Product.Application.Dtos.Requests;

/// <summary>
/// Dados para cadastro de produto.
/// </summary>
public record RegisterProductInput
{
    /// <summary>Nome do produto.</summary>
    [OpenApiField(Example = "Botijão P13")]
    public string Name { get; init; } = null!;

    /// <summary>Tipo do produto.</summary>
    [OpenApiField(
        Example = "Gas",
        AllowedValues = ["Water", "Gas"])]
    public string Type { get; init; } = null!;

    /// <summary>Preço unitário.</summary>
    [OpenApiField(Example = "89.90", Minimum = 0.01)]
    public decimal Price { get; init; }

    /// <summary>Quantidade inicial em estoque.</summary>
    [OpenApiField(Example = "50", Minimum = 0)]
    public int Quantity { get; init; }
}

using AquaGas.Shared.OpenApi;

namespace AquaGas.Sale.Application.Dtos.Requests;

/// <summary>
/// Item de uma venda (produto e quantidade).
/// </summary>
public record SaleItemsInput
{
    /// <summary>Identificador do produto.</summary>
    [OpenApiField(Format = "uuid")]
    public Guid ProductId { get; init; }

    /// <summary>Quantidade vendida.</summary>
    [OpenApiField(Example = "2", Minimum = 1)]
    public int Quantity { get; init; }
}

using AquaGas.Shared.OpenApi;

namespace AquaGas.Product.Application.Dtos.Requests;

/// <summary>
/// Movimentação de estoque de um produto.
/// </summary>
public record UpdateStockInput
{
    /// <summary>Tipo de movimentação.</summary>
    [OpenApiField(
        Example = "Entry",
        AllowedValues = ["Entry", "Exit"])]
    public string StockMovementType { get; init; } = null!;

    /// <summary>Quantidade movimentada (maior que zero).</summary>
    [OpenApiField(Example = "20", Minimum = 1)]
    public int Quantity { get; init; }

    /// <summary>Motivo da movimentação (mínimo 3 caracteres).</summary>
    [OpenApiField(Example = "Reposição de estoque", MinLength = 3)]
    public string Reason { get; init; } = null!;
}

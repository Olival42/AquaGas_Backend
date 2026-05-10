namespace AquaGas.Product.Application.Dtos.Requests;

public record UpdateStockInput
{
    public string StockMovementType { get; init; } = null!;
    public int Quantity { get; init; }
    public string Reason { get; init; } = null!;
}
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Product.Application.Dtos.Requests;

public record UpdateStockValidated
{
    public StockMovementType StockMovementType { get; init; }
    public StockQuantity Quantity { get; init; } = null!;
    public string Reason { get; init; } = null!;
}
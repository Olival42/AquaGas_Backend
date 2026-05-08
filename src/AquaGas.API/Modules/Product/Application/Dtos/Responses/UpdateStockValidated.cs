using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;

namespace AquaGas.Api.Modules.Product.Application.Dtos.Requests;

public record UpdateStockValidated
{
    public StockMovementType StockMovementType { get; init; }
    public StockQuantity Quantity { get; init; } = null!;
    public string Reason { get; init; } = null!;
}
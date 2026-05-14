namespace AquaGas.Sale.Application.Dtos.Responses;

using AquaGas.Shared.Domain.ValueObjects;

public sealed record RegisterSaleItemValidated
{
    public Guid ProductId { get; init; }

    public StockQuantity Quantity { get; init; } = null!;
}
namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record SaleItemResponse{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal Total { get; init; }
}
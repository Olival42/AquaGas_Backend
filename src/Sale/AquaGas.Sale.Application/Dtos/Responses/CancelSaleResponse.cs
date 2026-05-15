using AquaGas.Sale.Domain.Models;

namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record CancelSaleResponse {
    public Guid SaleId { get; init; }
    public SaleStatus Status { get; init; }
    public string Reason { get; init; } = null!;
}
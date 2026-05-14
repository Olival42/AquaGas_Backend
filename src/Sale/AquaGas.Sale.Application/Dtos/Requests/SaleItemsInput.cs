using System.Text.Json.Serialization;
using AquaGas.Shared.Serialization;

namespace AquaGas.Sale.Application.Dtos.Requests;

public record SaleItemsInput
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
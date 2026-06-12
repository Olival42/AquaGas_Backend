using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record RegisterSaleValidated
{
    public Guid? CustomerId { get; init; }

    public Discount? Discount { get; init; }

    public List<RegisterSaleItemValidated> SaleItems { get; init; } = [];
}

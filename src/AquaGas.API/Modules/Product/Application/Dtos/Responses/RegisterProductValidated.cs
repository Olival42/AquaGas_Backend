namespace AquaGas.Api.Modules.Product.Application.Dtos.Resposes;

using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;

public record RegisterProductValidated
{
    public ProductName Name { get; init; } = null!;
    public TypeProduct Type { get; init; }
    public Price Price { get; init; } = null!;
    public StockQuantity Quantity { get; init; } = null!;
}
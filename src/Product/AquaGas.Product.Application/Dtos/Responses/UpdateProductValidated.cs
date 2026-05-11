namespace AquaGas.Product.Application.Dtos.Resposes;

using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.ValueObjects;

public record UpdateProductValidated
{
    public ProductName? Name { get; init; }
    public Price? Price { get; init; }
    public TypeProduct? Type { get; init; }
}
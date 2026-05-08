namespace AquaGas.Api.Modules.Product.Application.Dtos.Resposes;

using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;

public record UpdateProductValidated
{
    public ProductName? Name { get; init; }
    public Price? Price { get; init; }
    public TypeProduct? Type { get; init; }
}
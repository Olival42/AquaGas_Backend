using AquaGas.Api.Modules.Product.Domain.Enums;

namespace AquaGas.Api.Modules.Product.Application.Dtos.Responses;

public record ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public TypeProduct Type { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}
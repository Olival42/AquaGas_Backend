namespace AquaGas.Api.Modules.Product.Application.Dtos.Requests;

public record RegisterProductInput
{
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}
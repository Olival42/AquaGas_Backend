namespace AquaGas.Api.Modules.Product.Application.Dtos.Requests;

public record UpdateProductInput
{
    public string? Name { get; init; }
    public decimal? Price { get; init; }
    public string? Type { get; init; }
}
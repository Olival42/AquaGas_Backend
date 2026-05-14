namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record CustomerSaleResponse{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Document { get; init; } = null!;
}
namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record EmployeeSaleResponse{
    public Guid Id { get; init; }
    public string Name  { get; init; } = null!;
}
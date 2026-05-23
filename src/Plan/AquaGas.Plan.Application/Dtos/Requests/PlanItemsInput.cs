namespace AquaGas.Plan.Application.Dtos.Requests;

public record PlanItemsInput
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
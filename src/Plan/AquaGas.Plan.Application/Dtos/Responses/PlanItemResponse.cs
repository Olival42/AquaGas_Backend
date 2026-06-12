namespace AquaGas.Plan.Application.Dtos.Responses;

public record PlanItemResponse
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
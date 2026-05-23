using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Dtos.Responses;

public record RegisterPlanItemValidated
{
    public Guid ProductId { get; set; }

    public StockQuantity Quantity { get; set; } = null!;
}
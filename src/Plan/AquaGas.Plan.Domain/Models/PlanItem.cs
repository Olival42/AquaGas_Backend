using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Domain.Models;

public sealed class PlanItem
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public Guid ProductId { get; private set; }

    public StockQuantity Quantity { get; private set; } = null!;


    private PlanItem() { }

    public PlanItem(
        Guid planId,
        Guid productId,
        StockQuantity quantity)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        ProductId = productId;
        Quantity = quantity;
    }

    public void UpdateQuantity(StockQuantity quantity)
    {
        Quantity = quantity;
    }
}
using AquaGas.Product.Domain.Enums;

namespace AquaGas.Product.Domain.Models;

public sealed class StockMovement
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StockMovement(
        Guid id,
        Guid productId,
        StockMovementType type,
        int quantity,
        string reason,
        Guid? referenceId,
        Guid createdBy,
        DateTime createdAt)
    {
        Id = id;
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Reason = reason;
        ReferenceId = referenceId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public static StockMovement Create(
        Guid productId,
        StockMovementType type,
        int quantity,
        string reason,
        Guid createdBy,
        Guid? referenceId = null)
    {
        return new StockMovement(
            Guid.NewGuid(),
            productId,
            type,
            quantity,
            reason.Trim(),
            referenceId,
            createdBy,
            DateTime.UtcNow
        );
    }
}
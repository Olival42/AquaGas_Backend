namespace AquaGas.Sale.Domain.Models;

using AquaGas.Shared.Domain.ValueObjects;

public sealed class SaleItem
{
    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Guid ProductId { get; private set; }

    public StockQuantity Quantity { get; private set; } = null!;

    public Price TotalPrice { get; private set; } = null!;

    public Sale? Sale { get; private set; }

    private SaleItem() { }

    public SaleItem(
        Guid saleId,
        Guid productId,
        StockQuantity quantity,
        Price totalPrice)
    {
        Id = Guid.NewGuid();
        SaleId = saleId;
        ProductId = productId;
        Quantity = quantity;
        TotalPrice = totalPrice;
    }

    public void UpdateQuantity(StockQuantity quantity)
    {
        Quantity = quantity;
    }

    public void UpdateTotalPrice(Price totalPrice)
    {
        TotalPrice = totalPrice;
    }
}
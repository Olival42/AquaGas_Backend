namespace AquaGas.Sale.Domain.Models;

using AquaGas.Shared.Domain.ValueObjects;

public sealed class Sale
{
    public Guid Id { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Price Total { get; private set; } = null!;

    public Discount? CurrentDiscount { get; private set; }

    public DateTime Date { get; private set; }

    public SaleStatus Status { get; private set; }

    public string? CancelReason { get; private set; }  = null!;

    private readonly List<SaleItem> _items = [];

    public IReadOnlyCollection<SaleItem> Items
        => _items.AsReadOnly();

    private Sale() { }

    public Sale(
        Guid? customerId,
        Guid employeeId,
        Price total,
        Discount? currentDiscount,
        DateTime? date = null)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        EmployeeId = employeeId;
        Total = total;
        CurrentDiscount = currentDiscount;
        Date = date ?? DateTime.UtcNow;
        Status = SaleStatus.Finished;
    }

    public void AddItem(SaleItem item)
    {
        _items.Add(item);
    }

    public void Cancel(string reason)
    {
        Status = SaleStatus.Canceled;
        CancelReason = reason;
    }

    public void UpdateTotal(Price total)
    {
        Total = total;
    }

    public void UpdateDiscount(Discount discount)
    {
        CurrentDiscount = discount;
    }
}
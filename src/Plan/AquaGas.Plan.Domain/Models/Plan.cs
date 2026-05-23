using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Domain.Models;

public sealed class Plan
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }
    public Guid EmployeeId { get; private set; }

    public PlanCycle Cycle { get; private set; }

    public Price Total { get; private set; } = null!;

    public Discount? CurrentDiscount { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public int DeliveryDay { get; private set; }

    public int BillingDay { get; private set; }

    public PlanStatus Status { get; private set; }

    public bool IsActive { get; private set; }

    public string? SuspensionReason { get; private set; }

    private readonly List<PlanItem> _items = [];

    public IReadOnlyCollection<PlanItem> Items
        => _items.AsReadOnly();

    private Plan() { }

    public Plan(
        Guid customerId,
        Guid employeeId,
        PlanCycle cycle,
        Price total,
        DateTime startDate,
        DateTime endDate,
        int deliveryDay,
        int billingDay,
        Discount? currentDiscount = null)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        EmployeeId = employeeId;
        Cycle = cycle;
        Total = total;
        CurrentDiscount = currentDiscount;
        StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        DeliveryDay = deliveryDay;
        BillingDay = billingDay;
        IsActive = true;
        Status = PlanStatus.Active;
    }

    public void AddItem(PlanItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(PlanItem item)
    {
        _items.Remove(item);
    }

    public void Cancel()
    {
        Status = PlanStatus.Canceled;
        IsActive = false;
    }

    public void Suspend(string reason)
    {
        if (Status == PlanStatus.Active)
        {
            Status = PlanStatus.Suspended;
            SuspensionReason = reason;
            IsActive = false;
        }
    }

    public void Reactivate()
    {
        if (Status == PlanStatus.Suspended)
        {
            Status = PlanStatus.Active;
            SuspensionReason = null;
            IsActive = true;
        }
    }

    public void UpdateDiscount(Discount discount)
    {
        CurrentDiscount = discount;
    }

    public void UpdateTotal(Price total)
    {
        Total = total;
    }

    public void SetAwaitingClosure()
    {
        if (Status != PlanStatus.Active)
            return;

        Status = PlanStatus.AwaitingClosure;
    }

    public void Finish()
    {
        if (Status != PlanStatus.Active &&
            Status != PlanStatus.AwaitingClosure)
            return;

        Status = PlanStatus.Finished;
        FinishedAt = DateTime.UtcNow;
        IsActive = false;
    }

    public void Upgrade(PlanCycle newCycle, Price newTotal, DateTime newEndDate)
    {
        if (Status != PlanStatus.Active)
            return;

        Cycle = newCycle;
        Total = newTotal;
        EndDate = DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc);
    }

    public void Downgrade(PlanCycle newCycle, Price newTotal, DateTime newEndDate)
    {
        if (Status != PlanStatus.Active)
            return;

        Cycle = newCycle;
        Total = newTotal;
        EndDate = DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc);
    }

    public void ClearItems()
    {
        _items.Clear();
    }
}

using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Domain.Models;

public sealed class Billing
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public int Period { get; private set; }

    public DateTime DueDate { get; private set; }

    public Price Amount { get; private set; } = null!;

    public BillingStatus Status { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public Guid? ReceivedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Billing() { }

    public Billing(
        Guid planId,
        int period,
        DateTime dueDate,
        Price amount)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        Period = period;
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
        Amount = amount;
        Status = BillingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Pay(Guid receivedBy)
    {
        if (Status == BillingStatus.Paid)
            return;

        Status = BillingStatus.Paid;
        PaidAt = DateTime.UtcNow;
        ReceivedBy = receivedBy;
    }

    public void Cancel()
    {
        if (Status == BillingStatus.Paid)
            return;

        if (Status == BillingStatus.Cancelled)
            return;

        Status = BillingStatus.Cancelled;
    }

    public void MarkAsLate()
    {
        if (Status != BillingStatus.Pending)
            return;

        if (DateTime.UtcNow > DueDate)
            Status = BillingStatus.Late;
    }

    public void Reschedule(DateTime newDate)
    {
        if (Status == BillingStatus.Pending || Status == BillingStatus.Late || Status == BillingStatus.Cancelled)
        {
            DueDate = DateTime.SpecifyKind(newDate, DateTimeKind.Utc);
            Status = BillingStatus.Pending;
        }
    }

    public void UpdateAmount(Price newAmount)
    {
        if (Status == BillingStatus.Pending || Status == BillingStatus.Late)
        {
            Amount = newAmount;
        }
    }
}
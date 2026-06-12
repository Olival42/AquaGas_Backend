using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Domain.Models;

public sealed class Delivery
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public int Period { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? DeliveryDate { get; private set; }

    public DeliveryStatus Status { get; private set; }

    public bool HasCustomSchedule { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Delivery() { }

    public Delivery(
        Guid planId,
        int period,
        DateTime dueDate)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        Period = period;
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
        Status = DeliveryStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != DeliveryStatus.Pending &&
            Status != DeliveryStatus.Late)
            return;

        DeliveryDate = DateTime.UtcNow;
        Status = DeliveryStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status == DeliveryStatus.Delivered)
            return;

        Status = DeliveryStatus.Cancelled;
    }

    public void MarkAsLate()
    {
        if (Status != DeliveryStatus.Pending)
            return;

        if (DateTime.UtcNow > DueDate)
            Status = DeliveryStatus.Late;
    }

    public void Reschedule(DateTime newDate)
    {
        if (Status == DeliveryStatus.Pending || Status == DeliveryStatus.Late || Status == DeliveryStatus.Cancelled)
        {
            DueDate = DateTime.SpecifyKind(newDate, DateTimeKind.Utc);
            Status = DeliveryStatus.Pending;
        }
    }

    public void RescheduleManually(DateTime newDate)
    {
        Reschedule(newDate);
        HasCustomSchedule = true;
    }
}
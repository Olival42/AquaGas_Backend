using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Domain.Models;

public sealed class ContractPenalty
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public Guid InitiatedBy { get; private set; }

    public ContractPenaltyType Type { get; private set; }

    public Price OriginalValue { get; private set; } = null!;

    public Price RemainingValue { get; private set; } = null!;

    public Price CalculatedAmount { get; private set; } = null!;

    public ContractPenaltyStatus Status { get; private set; }

    public DateTime Timestamp { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? PaidDate { get; private set; }

    public Guid? PaidBy { get; private set; }

    public Guid? WaivedBy { get; private set; }

    public DateTime? WaivedAt { get; private set; }

    public string? WaiveReason { get; private set; }

    public Guid? CanceledBy { get; private set; }

    public DateTime? CanceledAt { get; private set; }

    public string? CancelReason { get; private set; }

    public string? Notes { get; private set; }

    private ContractPenalty() { }

    public ContractPenalty(
        Guid planId,
        Guid customerId,
        Guid initiatedBy,
        ContractPenaltyType type,
        Price originalValue,
        Price remainingValue,
        Price calculatedAmount,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        InitiatedBy = initiatedBy;
        Type = type;
        OriginalValue = originalValue;
        RemainingValue = remainingValue;
        CalculatedAmount = calculatedAmount;
        Notes = string.IsNullOrWhiteSpace(notes) ? "Multa contratual gerada automaticamente." : notes;
        Status = ContractPenaltyStatus.PendingPayment;
        Timestamp = DateTime.UtcNow.Date;
        DueDate = Timestamp.AddDays(15).Date;
    }

    public void Pay(Guid paidBy)
    {
        if (Status != ContractPenaltyStatus.PendingPayment && Status != ContractPenaltyStatus.Overdue)
            return;

        Status = ContractPenaltyStatus.Paid;
        PaidDate = DateTime.UtcNow;
        PaidBy = paidBy;
    }

    public void Waive(Guid waivedBy, string reason)
    {
        if (Status != ContractPenaltyStatus.PendingPayment && Status != ContractPenaltyStatus.Overdue)
            return;

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            return;

        Status = ContractPenaltyStatus.Waived;
        WaivedAt = DateTime.UtcNow;
        WaivedBy = waivedBy;
        WaiveReason = reason;
        Notes = $"Isenção: {reason}";
    }

    public void Cancel(Guid userId, string reason)
    {
        if (Status != ContractPenaltyStatus.PendingPayment && Status != ContractPenaltyStatus.Overdue)
            return;

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10 || reason.Length > 500)
            return;

        Status = ContractPenaltyStatus.Canceled;
        CanceledAt = DateTime.UtcNow;
        CanceledBy = userId;
        CancelReason = reason;
        Notes = $"Cancelamento: {reason}";
    }

    public void MarkAsOverdue()
    {
        if (Status == ContractPenaltyStatus.PendingPayment && DateTime.UtcNow.Date > DueDate)
            Status = ContractPenaltyStatus.Overdue;
    }
}
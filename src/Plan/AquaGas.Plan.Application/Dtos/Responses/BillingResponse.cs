namespace AquaGas.Plan.Application.Dtos.Responses;

using AquaGas.Plan.Domain.Enums;

public record BillingResponse
{
    public Guid Id { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Amount { get; set; }

    public BillingStatus Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public Guid? ReceivedBy { get; set; }
}
namespace AquaGas.Plan.Application.Dtos.Responses;

public record ConfirmBillingPaymentResponse
{
    public Guid BillingId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public Guid? ReceivedBy { get; set; }

    public string Message { get; set; } = null!;
}
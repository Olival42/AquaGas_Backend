namespace AquaGas.Plan.Application.Dtos.Requests;

public record ConfirmBillingPaymentInput
{
    public Guid BillingId { get; set; }
}

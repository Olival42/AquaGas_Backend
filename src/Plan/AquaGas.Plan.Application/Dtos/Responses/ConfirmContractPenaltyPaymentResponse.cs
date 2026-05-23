namespace AquaGas.Plan.Application.Dtos.Responses;

public record ConfirmContractPenaltyPaymentResponse
{
    public Guid PenaltyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
    public decimal Amount { get; init; }
    public string Message { get; init; } = string.Empty;
}
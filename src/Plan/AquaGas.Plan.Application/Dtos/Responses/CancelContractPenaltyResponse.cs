namespace AquaGas.Plan.Application.Dtos.Responses;

public record CancelContractPenaltyResponse
{
    public Guid PenaltyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime? CanceledAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

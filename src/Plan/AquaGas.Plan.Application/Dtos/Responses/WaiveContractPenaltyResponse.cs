namespace AquaGas.Plan.Application.Dtos.Responses;

public record WaiveContractPenaltyResponse
{
    public Guid PenaltyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime? WaivedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
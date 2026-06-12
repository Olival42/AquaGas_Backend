namespace AquaGas.Plan.Application.Dtos.Responses;

public record CancelPlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CanceledDeliveries { get; set; }
    public int CanceledBillings { get; set; }
    public PenaltyResponse? Penalty { get; set; }
    public string Message { get; set; } = string.Empty;
}
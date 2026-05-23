namespace AquaGas.Plan.Application.Dtos.Responses;

public record SuspendPlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CanceledDeliveries { get; set; }
    public int CanceledBillings { get; set; }
    public string? Reason { get; set; }
    public string Message { get; set; } = string.Empty;
}
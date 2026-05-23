namespace AquaGas.Plan.Application.Dtos.Responses;

public record ReactivatePlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RescheduledDeliveries { get; set; }
    public int RescheduledBillings { get; set; }
    public string Message { get; set; } = string.Empty;
}
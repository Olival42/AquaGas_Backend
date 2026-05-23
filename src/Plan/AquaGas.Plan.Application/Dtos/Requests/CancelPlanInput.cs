namespace AquaGas.Plan.Application.Dtos.Requests;

public record CancelPlanInput
{
    public string? Reason { get; set; }
}
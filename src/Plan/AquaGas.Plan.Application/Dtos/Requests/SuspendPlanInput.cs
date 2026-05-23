namespace AquaGas.Plan.Application.Dtos.Requests;

public record SuspendPlanInput
{
    public string Reason { get; set; } = null!;
}

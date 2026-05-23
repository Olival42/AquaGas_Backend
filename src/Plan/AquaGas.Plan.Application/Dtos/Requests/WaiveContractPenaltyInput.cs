namespace AquaGas.Plan.Application.Dtos.Requests;

public record WaiveContractPenaltyInput
{
    public string Reason { get; init; } = string.Empty;
}
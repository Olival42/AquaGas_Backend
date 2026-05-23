namespace AquaGas.Plan.Application.Dtos.Requests;

public record CancelContractPenaltyInput
{
    public string Reason { get; init; } = string.Empty;
}

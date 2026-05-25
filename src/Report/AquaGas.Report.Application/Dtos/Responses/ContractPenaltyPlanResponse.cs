namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltyPlanResponse
{
    public Guid Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Cycle { get; set; } = string.Empty;
}

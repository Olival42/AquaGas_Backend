namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltyAuditResponse
{
    public bool CanBePaid { get; set; }

    public bool CanBeWaived { get; set; }

    public bool CanBeCanceled { get; set; }
}

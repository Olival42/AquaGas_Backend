namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltyReportResponse
{
    public ContractPenaltySummaryResponse Summary { get; set; } = new();

    public List<ContractPenaltyReportItemResponse> Items { get; set; } = [];
}

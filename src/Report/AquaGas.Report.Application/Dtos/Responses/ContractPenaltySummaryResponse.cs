namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltySummaryResponse
{
    public int TotalPenalties { get; set; }

    public decimal PendingAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal WaivedAmount { get; set; }

    public decimal OverdueAmount { get; set; }
}

namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltyFinancialResponse
{
    public decimal OriginalValue { get; set; }

    public decimal RemainingValue { get; set; }

    public decimal CalculatedValue { get; set; }
}

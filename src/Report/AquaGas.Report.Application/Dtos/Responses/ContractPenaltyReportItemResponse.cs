namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class ContractPenaltyReportItemResponse
{
    public string Id { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public ContractPenaltyPlanResponse Plan { get; set; } = new();

    public CustomerSalesResponse Customer { get; set; } = new();

    public ContractPenaltyOriginResponse Origin { get; set; } = new();

    public string Type { get; set; } = string.Empty;

    public ContractPenaltyFinancialResponse Financial { get; set; } = new();

    public string Status { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public EmployeeSalesResponse CreatedBy { get; set; } = new();

    public EmployeeSalesResponse? ResolvedBy { get; set; }

    public ContractPenaltyAuditResponse Audit { get; set; } = new();
}

namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class SalesReportItemResponse
{
    public string Id { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public CustomerSalesResponse? Customer { get; set; }

    public EmployeeSalesResponse Employee { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ItemsCount { get; set; }

    public decimal Total { get; set; }

    public List<SalesProductItemResponse> Items { get; set; } = [];
}

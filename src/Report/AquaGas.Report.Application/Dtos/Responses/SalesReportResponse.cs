namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class SalesReportResponse
{
    public SalesSummaryResponse Summary { get; set; } = new();

    public List<SalesReportItemResponse> Items { get; set; } = [];
}

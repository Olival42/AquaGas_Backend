namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class StockMovementReportResponse
{
    public List<StockMovementItemResponse> Items { get; set; } = [];
}

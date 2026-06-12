namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class SalesSummaryResponse
{
    public decimal TotalSpotSales { get; set; }

    public decimal TotalContractSales { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalSales { get; set; }

    public int CancelledSales { get; set; }

    public decimal AverageTicket { get; set; }

    public SalesPeriodResponse Period { get; set; } = new();
}

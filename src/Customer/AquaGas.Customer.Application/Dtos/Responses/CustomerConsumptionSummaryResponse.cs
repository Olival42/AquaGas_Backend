namespace AquaGas.Customer.Application.Dtos.Responses;

public sealed class CustomerConsumptionSummaryResponse
{
    public int TotalSales { get; set; }

    public int TotalDeliveries { get; set; }

    public decimal TotalSpent { get; set; }

    public int TotalItems { get; set; }

    public decimal AverageTicket { get; set; }

    public CustomerConsumptionPeriodResponse Period { get; set; } = new();
}

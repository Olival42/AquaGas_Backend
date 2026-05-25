namespace AquaGas.Report.Application.Configuration;

public sealed class ReportSettings : IReportSettings
{
    public int MaxSalesReportIntervalDays { get; init; } = 366;
}

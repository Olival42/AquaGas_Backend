namespace AquaGas.Plan.Application.Services;

using AquaGas.Plan.Domain.Enums;

public sealed class PlanDateService
    : IPlanDateService
{
    public DateTime GenerateStartDate(
        int deliveryDay,
        int billingDay)
    {
        var now = DateTime.UtcNow;

        var minLeadTimeDays = 3;
        var earliestAllowedDate = now.AddDays(minLeadTimeDays);

        var referenceDay =
            Math.Min(deliveryDay, billingDay);

        var startDate = AdjustDay(
            now.Year,
            now.Month,
            referenceDay);

        if (startDate.Date < earliestAllowedDate.Date)
        {
            var nextMonth = now.AddMonths(1);

            startDate = AdjustDay(
                nextMonth.Year,
                nextMonth.Month,
                referenceDay);
        }

        return startDate;
    }

    public DateTime GenerateEndDate(
        DateTime startDate,
        PlanCycle cycle,
        int deliveryDay,
        int billingDay,
        int? customMonths)
    {
        var monthsToAdd = cycle switch
        {
            PlanCycle.Monthly => 0,
            PlanCycle.Quarterly => 2,
            PlanCycle.Annual => 11,
            PlanCycle.Custom => (customMonths ?? 1) - 1,
            _ => 0
        };

        var targetMonthDate = startDate.AddMonths(monthsToAdd);
        var lastDayOfContract = Math.Max(deliveryDay, billingDay);

        return AdjustDay(
            targetMonthDate.Year,
            targetMonthDate.Month,
            lastDayOfContract);
    }

    public DateTime AdjustDay(
        int year,
        int month,
        int desiredDay)
    {
        var maxDay = DateTime.DaysInMonth(year, month);

        var validDay = Math.Min(desiredDay, maxDay);

        return new DateTime(year, month, validDay, 0, 0, 0, DateTimeKind.Utc);
    }
}
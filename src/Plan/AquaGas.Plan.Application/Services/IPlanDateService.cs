namespace AquaGas.Plan.Application.Services;

using AquaGas.Plan.Domain.Enums;

public interface IPlanDateService
{
    DateTime GenerateStartDate(
        int deliveryDay,
        int billingDay);

    DateTime GenerateEndDate(
        DateTime startDate,
        PlanCycle cycle,
        int deliveryDay,
        int billingDay,
        int? customMonths);

    DateTime AdjustDay(
        int year,
        int month,
        int desiredDay);
}
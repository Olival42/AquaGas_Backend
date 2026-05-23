using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Services;

public sealed class PlanBillingService : IPlanBillingService
{
    private readonly IPlanDateService _dateService;

    public PlanBillingService(IPlanDateService dateService)
    {
        _dateService = dateService;
    }

    public List<Billing> GenerateBillings(
        Guid planId,
        DateTime startDate,
        DateTime endDate,
        PlanCycle cycle,
        int billingDay,
        Price totalAmount,
        int? customMonths)
    {
        var billings = new List<Billing>();

        var current = startDate;
        var period = 1;

        while (current <= endDate)
        {
            var dueDate = _dateService.AdjustDay(
                current.Year,
                current.Month,
                billingDay);

            var amount = Price.Create(totalAmount.Value).Value!;

            billings.Add(new Billing(
                planId,
                period,
                dueDate,
                amount));

            current = current.AddMonths(1);

            period++;
        }

        return billings;
    }
}
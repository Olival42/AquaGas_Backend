using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;

namespace AquaGas.Plan.Application.Services;

public sealed class PlanDeliveryService
    : IPlanDeliveryService
{
    private readonly IPlanDateService _dateService;

    public PlanDeliveryService(
        IPlanDateService dateService)
    {
        _dateService = dateService;
    }

    public List<Delivery> GenerateDeliveries(
        Guid planId,
        DateTime startDate,
        DateTime endDate,
        PlanCycle cycle,
        int deliveryDay,
        int? customMonths)
    {
        var deliveries = new List<Delivery>();

        var current = startDate;

        int period = 1;

        while (current <= endDate)
        {
            var dueDate = _dateService.AdjustDay(
                current.Year,
                current.Month,
                deliveryDay);

            deliveries.Add(new Delivery(
                planId,
                period,
                dueDate));

            current = current.AddMonths(1);

            period++;
        }

        return deliveries;
    }
}
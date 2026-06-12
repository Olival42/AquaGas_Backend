using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Services;

public sealed class PlanFactoryService
    : IPlanFactoryService
{
    public Domain.Models.Plan CreatePlan(
        RegisterPlanInput input,
        Guid employeeId,
        PlanCycle cycle,
        Price total,
        Discount? discount,
        DateTime startDate,
        DateTime endDate,
        int deliveryDay,
        int billingDay)
    {
        return new Domain.Models.Plan(
            input.CustomerId,
            employeeId,
            cycle,
            total,
            startDate,
            endDate,
            deliveryDay,
            billingDay,
            discount);
    }

    public List<PlanItem> CreateItems(
        Guid planId,
        List<RegisterPlanItemValidated> items)
    {
        var planItems = new List<PlanItem>();

        foreach (var item in items)
        {
            planItems.Add(new PlanItem(
                planId,
                item.ProductId,
                item.Quantity));
        }

        return planItems;
    }
}
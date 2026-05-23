namespace AquaGas.Plan.Application.Services;

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Plan.Application.Dtos.Responses;

public interface IPlanFactoryService
{
    Domain.Models.Plan CreatePlan(
        RegisterPlanInput input,
        Guid employeeId,
        PlanCycle cycle,
        Price total,
        Discount? discount,
        DateTime startDate,
        DateTime endDate,
        int deliveryDay,
        int billingDay);

    List<PlanItem> CreateItems(
        Guid planId,
        List<RegisterPlanItemValidated> items);
}
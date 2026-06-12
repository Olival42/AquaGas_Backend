namespace AquaGas.Plan.Application.Services;

using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

public interface IPlanBillingService
{
    List<Billing> GenerateBillings(
        Guid planId,
        DateTime startDate,
        DateTime endDate,
        PlanCycle cycle,
        int billingDay,
        Price amount,
        int? customMonths);
}
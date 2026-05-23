namespace AquaGas.Plan.Application.Services;

using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Enums;

public interface IPlanDeliveryService
{
    List<Delivery> GenerateDeliveries(
        Guid planId,
        DateTime startDate,
        DateTime endDate,
        PlanCycle cycle,
        int deliveryDay, 
        int? customMonths);
}
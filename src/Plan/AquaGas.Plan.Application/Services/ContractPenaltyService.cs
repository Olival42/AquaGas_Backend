using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Application.Services;

public sealed class ContractPenaltyService : IContractPenaltyService
{
    public ContractPenalty? CalculateCancellationPenalty(
        PlanEntity plan,
        List<Billing> billings,
        Guid userId,
        string? notes = null)
    {
        var remainingBillings = billings
            .Where(x => x.Status == BillingStatus.Pending ||
                       x.Status == BillingStatus.Late)
            .ToList();

        var sum = remainingBillings.Sum(x => x.Amount.Value);
        if (sum <= 0)
            return null;

        var originalValue = Price.Create(plan.Total.Value).Value!;
        var remainingValue = Price.Create(sum).Value!;
        var calculatedAmount = Price.Create(sum).Value!;

        return new ContractPenalty(
            plan.Id,
            plan.CustomerId,
            userId,
            ContractPenaltyType.EarlyCancellation,
            originalValue,
            remainingValue,
            calculatedAmount,
            notes);
    }
}
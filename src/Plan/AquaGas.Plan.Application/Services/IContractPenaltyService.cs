using AquaGas.Plan.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Application.Services;

public interface IContractPenaltyService
{
    ContractPenalty? CalculateCancellationPenalty(
        PlanEntity plan,
        List<Billing> billings,
        Guid userId,
        string? notes = null);
}
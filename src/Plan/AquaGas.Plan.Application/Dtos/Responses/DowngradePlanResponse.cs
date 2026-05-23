using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Application.Dtos.Responses;

public record DowngradePlanResponse
{
    public Guid PlanId { get; init; }

    public decimal PreviousTotal { get; init; }

    public decimal NewTotal { get; init; }

    public PlanCycle PreviousCycle { get; init; }

    public PlanCycle Cycle { get; init; }

    public int UpdatedBillings { get; init; }

    public int UpdatedDeliveries { get; init; }

    public int CanceledBillings { get; init; }

    public int CanceledDeliveries { get; init; }

    public int UpdatedItems { get; init; }

    public int RemovedItems { get; init; }

    public PenaltyResponse? Penalty { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

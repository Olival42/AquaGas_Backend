using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Application.Dtos.Requests;

public record UpgradePlanInput
{
    public PlanCycle? Cycle { get; init; }

    public int? DurationInMonths { get; init; }

    public string? Reason { get; init; }

    public List<UpgradePlanItemInput>? Items { get; init; }
}

public record UpgradePlanItemInput
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }
}

using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Application.Dtos.Requests;

public record DowngradePlanInput
{
    public PlanCycle? Cycle { get; init; }

    public int? DurationInMonths { get; init; }

    public string Reason { get; init; } = string.Empty;

    public List<DowngradePlanItemInput>? Items { get; init; }
}

public record DowngradePlanItemInput
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }
}

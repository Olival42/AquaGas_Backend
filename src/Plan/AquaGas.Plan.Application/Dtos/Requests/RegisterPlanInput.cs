namespace AquaGas.Plan.Application.Dtos.Requests;

public record RegisterPlanInput
{
    public Guid CustomerId { get; init; }
    public string Cycle { get; init; } = null!;
    public double? Discount { get; init; }
    public int DeliveryDay { get; init; }
    public int BillingDay { get; init; }
    public int? DurationInMonths { get; init; }
    public bool IgnoreWarnings { get; init; }
    public List<PlanItemsInput> Items { get; init; } = null!;
}

using AquaGas.Plan.Domain.Enums;

using AquaGas.Shared.Domain.ValueObjects;

namespace AquaGas.Plan.Application.Dtos.Responses;

public record RegisterPlanValidated
{
    public Guid CustomerId { get; set; }

    public PlanCycle Cycle { get; set; }

    public Discount? Discount { get; set; }

    public int DeliveryDay { get; set; }

    public int BillingDay { get; set; }

    public int? DurationInMonths { get; set; }

    public List<RegisterPlanItemValidated> Items { get; set; } = [];
}
namespace AquaGas.Plan.Application.Dtos.Responses;

using AquaGas.Plan.Domain.Enums;

public record PlanResponse
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public PlanCycle Cycle { get; set; }

    public PlanStatus Status { get; set; }

    public decimal Total { get; set; }

    public double? Discount { get; set; }

    public int DeliveryDay { get; set; }

    public int BillingDay { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Warning { get; set; }

    public List<PlanItemResponse> Items { get; set; } = [];

    public List<DeliveryResponse> Deliveries { get; set; } = [];

    public List<BillingResponse> Billings { get; set; } = [];

    public List<PenaltyResponse> Penalties { get; set; } = [];
}
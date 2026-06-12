namespace AquaGas.Customer.Application.Dtos.Responses;

public sealed class CustomerConsumptionItemResponse
{
    public string OriginId { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Type { get; set; } = string.Empty;

    public Guid? PlanId { get; set; }

    public IReadOnlyList<CustomerConsumptionProductResponse> Products { get; set; }
        = [];

    public int Quantity { get; set; }

    public decimal Value { get; set; }
}

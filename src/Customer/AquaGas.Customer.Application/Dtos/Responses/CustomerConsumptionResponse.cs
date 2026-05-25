namespace AquaGas.Customer.Application.Dtos.Responses;

public sealed class CustomerConsumptionResponse
{
    public CustomerConsumptionSummaryResponse Summary { get; set; } = new();

    public IReadOnlyList<CustomerConsumptionItemResponse> Items { get; set; }
        = [];
}

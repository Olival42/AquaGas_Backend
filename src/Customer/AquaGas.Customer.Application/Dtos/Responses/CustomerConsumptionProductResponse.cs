namespace AquaGas.Customer.Application.Dtos.Responses;

public sealed class CustomerConsumptionProductResponse
{
    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;
}

namespace AquaGas.Plan.Application.Dtos.Requests;

public record CancelDeliveryInput
{
    public Guid DeliveryId { get; set; }
    public string Reason { get; set; } = null!;
}

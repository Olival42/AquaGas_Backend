namespace AquaGas.Plan.Application.Dtos.Requests;

public record RescheduleDeliveryInput
{
    public Guid DeliveryId { get; set; }
    public DateTime NewDate { get; set; }
    public string Reason { get; init; } = null!;
}

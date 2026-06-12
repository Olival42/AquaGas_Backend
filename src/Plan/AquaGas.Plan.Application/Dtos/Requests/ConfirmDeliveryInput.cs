namespace AquaGas.Plan.Application.Dtos.Requests;

public record ConfirmDeliveryInput
{
    public Guid DeliveryId { get; set; }
}

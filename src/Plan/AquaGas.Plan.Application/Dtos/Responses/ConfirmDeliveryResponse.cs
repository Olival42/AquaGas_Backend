namespace AquaGas.Plan.Application.Dtos.Responses;

public record ConfirmDeliveryResponse
{
    public Guid DeliveryId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? DeliveryDate { get; set; }

    public string Message { get; set; } = null!;
}
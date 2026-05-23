namespace AquaGas.Plan.Application.Dtos.Responses;

public record RescheduleDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public DateTime PreviousDate { get; set; }
    public DateTime NewDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
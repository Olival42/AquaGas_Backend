namespace AquaGas.Plan.Application.Dtos.Responses;

public record CancelDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Message { get; set; } = string.Empty;
}
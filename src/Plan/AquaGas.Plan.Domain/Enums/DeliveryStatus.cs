namespace AquaGas.Plan.Domain.Enums;

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Late,
    Cancelled,
    Canceled = Cancelled
}
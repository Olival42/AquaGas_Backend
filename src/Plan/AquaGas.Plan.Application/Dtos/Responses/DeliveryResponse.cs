namespace AquaGas.Plan.Application.Dtos.Responses;

using AquaGas.Plan.Domain.Enums;

public record DeliveryResponse
{
    public Guid Id { get; set; }

    public int Period { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DeliveryStatus Status { get; set; }
}
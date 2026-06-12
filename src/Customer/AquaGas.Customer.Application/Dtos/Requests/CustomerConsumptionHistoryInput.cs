namespace AquaGas.Customer.Application.Dtos.Requests;

public sealed class CustomerConsumptionHistoryInput
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

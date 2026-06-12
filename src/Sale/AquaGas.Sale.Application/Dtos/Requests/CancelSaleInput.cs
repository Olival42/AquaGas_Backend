namespace AquaGas.Sale.Application.Dtos.Requests;

public record class CancelSaleInput
{
    public string Reason { get; set; } = string.Empty;
}
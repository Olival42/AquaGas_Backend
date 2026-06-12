using AquaGas.Product.Domain.Enums;

namespace AquaGas.Report.Application.Dtos.Responses;

public sealed class StockMovementItemResponse
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public ProductMovementResponse Product { get; set; } = null!;

    public StockMovementType Type { get; set; }

    public int Quantity { get; set; }

    public string Reason { get; set; } = null!;

    public StockMovementReferenceResponse? Reference { get; set; }

    public EmployeeMovementResponse Employee { get; set; } = null!;

    public CustomerMovementResponse? Customer { get; set; }
}

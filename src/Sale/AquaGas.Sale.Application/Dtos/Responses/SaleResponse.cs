using AquaGas.Sale.Domain.Models;

namespace AquaGas.Sale.Application.Dtos.Responses;

public sealed record SaleResponse{
    public Guid Id  { get; init; }
    public CustomerSaleResponse? Customer { get; init; } 
    public EmployeeSaleResponse Employee { get; init; } = null!;
    public List<SaleItemResponse> Items { get; init; } = null!;
    public decimal Subtotal { get; init; }
    public double Discount { get; init; }
    public decimal Total  { get; init; }
    public SaleStatus Status { get; init; }
    public string CancelReason { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
};
using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Application.Dtos.Responses;

public record PenaltyResponse
{
    public Guid Id { get; init; }
    public Guid PlanId { get; init; }
    public ContractPenaltyType Type { get; init; }
    public decimal OriginalValue { get; init; }
    public decimal RemainingValue { get; init; }
    public decimal CalculatedAmount { get; init; }
    public ContractPenaltyStatus Status { get; init; }
    public DateTime Timestamp { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? PaidDate { get; init; }
    public Guid? PaidBy { get; init; }
    public Guid? WaivedBy { get; init; }
    public DateTime? WaivedAt { get; init; }
    public string? WaiveReason { get; init; }
    public Guid? CanceledBy { get; init; }
    public DateTime? CanceledAt { get; init; }
    public string? CancelReason { get; init; }
    public string? Notes { get; init; }
}
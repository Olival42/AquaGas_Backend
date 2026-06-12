namespace AquaGas.Plan.Domain.Enums;

public enum BillingStatus
{
    Pending,
    Paid,
    Late,
    Cancelled,
    Canceled = Cancelled
}
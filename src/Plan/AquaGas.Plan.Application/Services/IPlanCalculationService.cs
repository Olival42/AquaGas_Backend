namespace AquaGas.Plan.Application.Services;

using AquaGas.Shared.Domain.ValueObjects;

using AquaGas.Shared.Results;

public interface IPlanCalculationService
{
    Result<Price> CalculateContractTotal(
        decimal monthlySubtotal,
        int numberOfMonths,
        Discount? discount);

    Result<Price> CalculateMonthlyBilling(
        Price contractTotal,
        int numberOfBillings);
}
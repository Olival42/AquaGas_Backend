namespace AquaGas.Plan.Application.Services;

using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public sealed class PlanCalculationService
    : IPlanCalculationService
{
    public Result<Price> CalculateContractTotal(
        decimal monthlySubtotal,
        int numberOfMonths,
        Discount? discount)
    {
        decimal rawTotal = monthlySubtotal * numberOfMonths;

        if (discount is not null)
        {
            rawTotal -= rawTotal * ((decimal)discount.Value / 100);
        }

        return Price.Create(rawTotal);
    }

    public Result<Price> CalculateMonthlyBilling(
        Price contractTotal,
        int numberOfBillings)
    {
        if (numberOfBillings <= 0)
            return Result<Price>
                .Fail(Error.Validation("Number of billings must be greater than zero"));

        decimal monthlyValue = contractTotal.Value / numberOfBillings;

        return Price.Create(monthlyValue);
    }
}
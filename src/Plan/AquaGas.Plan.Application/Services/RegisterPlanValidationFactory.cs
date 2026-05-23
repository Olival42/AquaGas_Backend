using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Domain.Factories;

using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.Services;

public static class RegisterPlanValidationFactory
{
    public static Result<RegisterPlanValidated> Combine(
        RegisterPlanInput data)
    {
        var errors = new List<Error>();

        Discount? discount = null;

        if (data.Discount is { } discountInput)
        {
            var discountResult = Discount.Create(discountInput);

            if (discountResult.IsFailure)
                errors.AddRange(discountResult.Errors);
            else
                discount = discountResult.Value!;
        }

        var cycleResult = PlanCycleFactory.Create(data.Cycle);

        if (cycleResult.IsFailure)
            errors.AddRange(cycleResult.Errors);

        var items = new List<RegisterPlanItemValidated>();

        foreach (var item in data.Items)
        {
            var quantity = StockQuantity.Create(item.Quantity);

            if (quantity.IsFailure)
            {
                errors.AddRange(quantity.Errors);
                continue;
            }

            items.Add(
                new RegisterPlanItemValidated
                {
                    ProductId = item.ProductId,
                    Quantity = quantity.Value!
                });
        }

        if (errors.Count > 0)
        {
            return Result<RegisterPlanValidated>
                .Fail(errors.ToArray());
        }

        return Result<RegisterPlanValidated>
            .Success(new RegisterPlanValidated
                {
                    CustomerId = data.CustomerId,
                    Cycle = cycleResult.Value,
                    Discount = discount,
                    DeliveryDay = data.DeliveryDay,
                    BillingDay = data.BillingDay,
                    DurationInMonths = data.DurationInMonths,
                    Items = items
                });
    }
}
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.Services;

public static class RegisterSaleValidationFactory
{
    public static Result<RegisterSaleValidated> Combine(
        RegisterSaleInput data)
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

        var items = new List<RegisterSaleItemValidated>();

        foreach (var item in data.SaleItems)
        {
            var quantity = StockQuantity.Create(item.Quantity);

            if (quantity.IsFailure)
            {
                errors.AddRange(quantity.Errors);
                continue;
            }

            items.Add(new RegisterSaleItemValidated
            {
                ProductId = item.ProductId,
                Quantity = quantity.Value!
            });
        }

        if (errors.Count > 0)
            return Result<RegisterSaleValidated>
                .Fail(errors.ToArray());

        return Result<RegisterSaleValidated>
            .Success(new RegisterSaleValidated
            {
                CustomerId = data.CustomerId,
                Discount = discount,
                SaleItems = items
            });
    }
}

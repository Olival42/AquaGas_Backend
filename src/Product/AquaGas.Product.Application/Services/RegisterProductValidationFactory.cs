using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Resposes;
using AquaGas.Product.Domain.Factories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.Services;

public static class RegisterProductValidationFactory
{
    public static Result<RegisterProductValidated> Combine(RegisterProductInput data)
    {
        var name = ProductName.Create(data.Name);
        var price = Price.Create(data.Price);
        var quantity = StockQuantity.Create(data.Quantity);
        var typeProduct = TypeProductFactory.Create(data.Type);

        var result = Result.Combine(
            name, price, quantity, typeProduct
        );

        if (result.IsFailure)
            return Result<RegisterProductValidated>.Fail(result.Errors.ToArray());

        return Result<RegisterProductValidated>.Success(
            new RegisterProductValidated
            {
                Name = name.Value!,
                Price = price.Value!,
                Quantity = quantity.Value!,
                Type = typeProduct.Value!,
            }
        );
    }
}
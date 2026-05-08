namespace AquaGas.Api.Modules.Product.Domain.Factories;

using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

public static class TypeProductFactory
{
    public static Result<TypeProduct> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<TypeProduct>.Fail(
                Error.Validation("TypeProduct is required", "TypeProduct")
            );

        var value = input.Trim();

        return value switch
        {
            "Water" => Result<TypeProduct>.Success(TypeProduct.Water),
            "Gas" => Result<TypeProduct>.Success(TypeProduct.Gas),

            _ => Result<TypeProduct>.Fail(
                Error.Validation("Type of product is invalid. Allowed: Water, Gas", "TypeProduct")
            )
        };
    }
}

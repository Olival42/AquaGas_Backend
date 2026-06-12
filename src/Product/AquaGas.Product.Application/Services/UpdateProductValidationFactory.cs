using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Resposes;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Factories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.Services;

public static class UpdateProductValidationFactory
{
    public static Result<UpdateProductValidated> Combine(UpdateProductInput data)
    {
        var errors = new List<Error>();

        ProductName? name = null;
        Price? price = null;
        TypeProduct? type = null;

        if (data.Name is not null)
        {
            var result = ProductName.Create(data.Name);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else name = result.Value;
        }

        if (data.Price is not null)
        {
            var result = Price.Create(data.Price.Value);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else price = result.Value;
        }

        if (data.Type is not null)
        {
            var result = TypeProductFactory.Create(data.Type);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else type = result.Value;
        }

        if (data.Name is null && data.Price is null && data.Type is null)
        {
            errors.Add(
                Error.Validation("At least one field must be provided", "UpdateProductInput")
            );
        }

        if (errors.Any())
            return Result<UpdateProductValidated>.Fail(errors.ToArray());

        return Result<UpdateProductValidated>.Success(
            new UpdateProductValidated
            {
                Name = name,
                Price = price,
                Type = type
            }
        );
    }
}
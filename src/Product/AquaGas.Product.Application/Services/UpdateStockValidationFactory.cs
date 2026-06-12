using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Domain.Factories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.Services;

public static class UpdateStockValidationFactory
{
    public static Result<UpdateStockValidated> Combine(UpdateStockInput data)
    {
        var quantity = StockQuantity.Create(data.Quantity);
        var typeStockMovement = StockMovementTypeFactory.Create(data.StockMovementType);

        var result = Result.Combine(quantity, typeStockMovement);

        if (result.IsFailure)
            return Result<UpdateStockValidated>.Fail(result.Errors.ToArray());

        return Result<UpdateStockValidated>.Success(
            new UpdateStockValidated
            {
                Quantity = quantity.Value!,
                StockMovementType = typeStockMovement.Value!,
                Reason = data.Reason
            }
        );
    }
}
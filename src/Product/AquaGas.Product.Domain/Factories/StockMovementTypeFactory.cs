namespace AquaGas.Product.Domain.Factories;

using AquaGas.Product.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public static class StockMovementTypeFactory
{
    public static Result<StockMovementType> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<StockMovementType>.Fail(
                Error.Validation("StockMovementType is required", "StockMovementType")
            );

        var value = input.Trim();

        return value switch
        {
            "Entry" => Result<StockMovementType>.Success(StockMovementType.Entry),
            "Exit" => Result<StockMovementType>.Success(StockMovementType.Exit),

            _ => Result<StockMovementType>.Fail(
                Error.Validation("Type of movement stock is invalid. Allowed: Entry, Exit", "StockMovementType")
            )
        };
    }
}

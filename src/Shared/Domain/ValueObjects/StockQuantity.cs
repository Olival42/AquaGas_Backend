using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Shared.Domain.ValueObjects;

public sealed class StockQuantity : IEquatable<StockQuantity>
{
    public int Value { get; }

    private StockQuantity(int value)
    {
        Value = value;
    }

    public static Result<StockQuantity> Create(int quantity)
    {
        if (quantity < 0)
            return Result<StockQuantity>.Fail(
                Error.Validation("Stock quantity cannot be negative", "Quantity")
            );

        return Result<StockQuantity>.Success(new StockQuantity(quantity));
    }

    public Result<StockQuantity> Increase(int amount)
    {
        if (amount <= 0)
            return Result<StockQuantity>.Fail(
                Error.Validation("Increase amount must be greater than zero", "Quantity")
            );

        return Result<StockQuantity>.Success(new StockQuantity(Value + amount));
    }

    public Result<StockQuantity> Decrease(int amount)
    {
        if (amount <= 0)
            return Result<StockQuantity>.Fail(
                Error.Validation("Decrease amount must be greater than zero", "Quantity")
            );

        if (Value < amount)
            return Result<StockQuantity>.Fail(
                Error.InsufficientStock()
            );

        return Result<StockQuantity>.Success(new StockQuantity(Value - amount));
    }

    public StockQuantity Reset()
        => new StockQuantity(0);

    public override string ToString() => Value.ToString();

    public override bool Equals(object? obj)
        => obj is StockQuantity other && Equals(other);

    public bool Equals(StockQuantity? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(StockQuantity? left, StockQuantity? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(StockQuantity? left, StockQuantity? right)
        => !(left == right);

}
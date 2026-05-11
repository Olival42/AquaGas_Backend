using System.Globalization;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Domain.ValueObjects;

public sealed class Price : IEquatable<Price>
{
    public decimal Value { get; }

    private Price(decimal value)
    {
        Value = value;
    }

    public static Result<Price> Create(decimal price)
    {
        if (price <= 0)
            return Result<Price>.Fail(
                Error.Validation("Price must be greater than zero", "Price")
            );

        return Result<Price>.Success(new Price(decimal.Round(price, 2)));
    }

    public override string ToString()
        => Value.ToString("F2", CultureInfo.InvariantCulture);

    public override bool Equals(object? obj)
        => obj is Price other && Equals(other);

    public bool Equals(Price? other)
        => other is not null && Value == other.Value;

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(Price left, Price right)
        => Equals(left, right);

    public static bool operator !=(Price left, Price right)
        => !Equals(left, right);

}
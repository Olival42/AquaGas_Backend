namespace AquaGas.Shared.Domain.ValueObjects;

using System.Globalization;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public sealed record Discount
{
    public double Value { get; }

    private Discount(double value)
    {
        Value = value;
    }

    public static Result<Discount> Create(double value)
    {
        if (value <= 0)
            return Result<Discount>.Fail(
                Error.Validation("Discount must be greater than 0"));

        if (value > 100)
            return Result<Discount>.Fail(
                Error.Validation("Discount cannot be greater than 100"));

        return Result<Discount>.Success(new Discount(value));
    }

    public override string ToString()
    => $"{Value.ToString(CultureInfo.InvariantCulture)}%";
}
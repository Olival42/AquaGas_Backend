using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Shared.Domain.ValueObjects;

public sealed class Phone
{
    public string Value { get; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Result<Phone> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Phone>.Fail(
                Error.Validation("Phone cannot be empty")
            );
        }

        value = value.Replace(" ", "")
                     .Replace("(", "")
                     .Replace(")", "")
                     .Replace("-", "");

        if (value.Length < 10 || value.Length > 11)
        return Result<Phone>.Fail(
                Error.Validation("Invalid phone")
            );

        return Result<Phone>.Success(new Phone(value));
    }

    public override string ToString() => Value;
}
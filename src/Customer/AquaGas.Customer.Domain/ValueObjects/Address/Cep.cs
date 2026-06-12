using System.Text.RegularExpressions;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Domain.ValueObjects.Address;

public sealed class Cep : IEquatable<Cep>
{
    private static readonly Regex _regex = new(@"^\d{8}$");
    private static readonly Regex _onlyNumbers = new(@"\D", RegexOptions.Compiled);

    public string Value { get; }

    private Cep(string value)
    {
        Value = value;
    }

    public static Result<Cep> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Cep>.Fail(
                Error.Validation("Cep cannot be empty", "Cep")
            );

        value = _onlyNumbers.Replace(value, "");

        if (!_regex.IsMatch(value))
            return Result<Cep>.Fail(
                Error.Validation("Invalid Cep", "Cep")
            );

        return Result<Cep>.Success(new Cep(value));
    }

    public bool Equals(Cep? other)
        => other is not null && Value == other.Value;

    public override bool Equals(object? obj)
        => obj is Cep other && Equals(other);

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString() => Value;
}
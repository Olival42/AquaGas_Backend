using System.Text.RegularExpressions;
using AquaGas.Shared.Results;
using AquaGas.Shared.Errors;

namespace AquaGas.Customer.Domain.ValueObjects.Customer;

public sealed class Cnpj
{
    private static readonly Regex _onlyNumbers = new(@"\D", RegexOptions.Compiled);

    public string Value { get; }

    private Cnpj(string value)
    {
        Value = value;
    }

    public static Result<Cnpj> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Cnpj>.Fail(
                Error.Validation("CNPJ cannot be empty", "Cnpj")
            );

        var normalized = Normalize(value);

        if (normalized.Length != 14)
            return Result<Cnpj>.Fail(
                Error.Validation("Invalid CNPJ", "Cnpj")
            );

        if (AllDigitsEqual(normalized))
            return Result<Cnpj>.Fail(
                Error.Validation("Invalid CNPJ", "Cnpj")
            );

        if (!IsValid(normalized))
            return Result<Cnpj>.Fail(
                Error.Validation("Invalid CNPJ", "Cnpj")
            );

        return Result<Cnpj>.Success(new Cnpj(normalized));
    }

    private static string Normalize(string value)
        => _onlyNumbers.Replace(value, "");

    private static bool AllDigitsEqual(string cnpj)
        => cnpj.All(c => c == cnpj[0]);

    private static bool IsValid(string cnpj)
    {
        var numbers = cnpj.Select(c => c - '0').ToArray();

        var firstWeights = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var secondWeights = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum = 0;
        for (int i = 0; i < 12; i++)
            sum += numbers[i] * firstWeights[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        sum = 0;
        for (int i = 0; i < 13; i++)
            sum += numbers[i] * secondWeights[i];

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return numbers[12] == digit1 && numbers[13] == digit2;
    }

    public override string ToString() => Value;
}
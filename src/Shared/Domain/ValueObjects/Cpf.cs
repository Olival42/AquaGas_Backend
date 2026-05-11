using System.Text.RegularExpressions;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Shared.Domain.ValueObjects;

public sealed class Cpf
{
    public string Value { get; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Result<Cpf> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Cpf>.Fail(
                        Error.Validation("CPF cannot be empty", "CPF")
                    );

        value = value.Replace(".", "").Replace("-", "");

        if (!IsValid(value))
            return Result<Cpf>.Fail(
                        Error.Validation("Invalid CPF", "CPF")
                    );

        return Result<Cpf>.Success(new Cpf(value));
    }

    public override string ToString() => Value;

    private static bool IsValid(string value)
    {
        string cpf = Regex.Replace(value, @"\D", "");

        if (!Regex.IsMatch(cpf, @"^\d{11}$"))
            return false;

        if (cpf.Distinct().Count() == 1)
            return false;

        int reminderFirstDigit = CalculateDigit(cpf, 9);
        int reminderSecondDigit = CalculateDigit(cpf, 10);

        return reminderFirstDigit == (cpf[9] - '0')
            && reminderSecondDigit == (cpf[10] - '0');
    }

    private static int CalculateDigit(string value, int size)
    {
        int sum = 0;
        int multiplier = size + 1;

        for (int i = 0; i < size; i++)
        {
            sum += (value[i] - '0') * multiplier;
            multiplier--;
        }

        int remainder = sum * 10 % 11;

        return remainder == 10 ? 0 : remainder;
    }
}
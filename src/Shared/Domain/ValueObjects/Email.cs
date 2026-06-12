namespace AquaGas.Shared.Domain.ValueObjects;

using System.Text.RegularExpressions;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public sealed class Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Fail(
                    Error.Validation("Email cannot be empty", "Email")
                );

        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        if (!regex.IsMatch(value))
            return Result<Email>.Fail(
                        Error.Validation("Invalid email", "Email")
                    );

        return Result<Email>.Success(new Email(value.ToLower()));
    }

    public override string ToString() => Value;
}
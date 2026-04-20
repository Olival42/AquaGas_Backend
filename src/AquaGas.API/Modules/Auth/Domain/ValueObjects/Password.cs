namespace AquaGas.Api.Modules.Auth.Domain.ValueObjects;

using System.Text.RegularExpressions;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

public sealed class Password
{
    private static readonly Regex _regex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Result<Password> Create(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            return Result<Password>.Fail(
                Error.Validation("Password cannot empty")
            );

        if (!_regex.IsMatch(plainPassword))
            return Result<Password>.Fail(
               Error.Validation("Password weak")
            );

        return Result<Password>.Success(new Password(plainPassword));
    }

    public static Password FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash inválido.");

        return new Password(hash);
    }

    public static implicit operator string(Password password)
        => password.Value;
}
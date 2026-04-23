using System.Text.RegularExpressions;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Errors;

namespace AquaGas.Api.Modules.Auth.Domain.ValueObjects;

public sealed class UserName : IEquatable<UserName>
{
    private static readonly Regex _regex = new(
        @"^[a-zA-Z0-9]{3,50}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private UserName(string value)
    {
        Value = value.ToLower();
    }

    public static Result<UserName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<UserName>.Fail(
                Error.Validation("Username cannot be empty.", "UserName")
            );

        if (!_regex.IsMatch(value))
            return Result<UserName>.Fail(
                Error.Validation("Username must be between 3 and 50 characters and contain only letters and numbers.", "UserName")
            );

        return Result<UserName>.Success(new UserName(value));
    }

    public override string ToString() => Value;

    public bool Equals(UserName? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
        => obj is UserName other && Equals(other);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(UserName left, UserName right)
        => Equals(left, right);

    public static bool operator !=(UserName left, UserName right)
        => !Equals(left, right);
}
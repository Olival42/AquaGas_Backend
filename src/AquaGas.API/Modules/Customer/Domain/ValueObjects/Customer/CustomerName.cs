using System.Text.RegularExpressions;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;

public sealed class CustomerName
{
    public string Value { get; }

    private CustomerName(string value)
    {
        Value = value;
    }

    public static Result<CustomerName> Create(string input)
    {
        if (input is null)
            return Result<CustomerName>.Fail(
                Error.Validation("Name is required", "Name")
            );

        var value = Normalize(input);

        if (string.IsNullOrWhiteSpace(value))
            return Result<CustomerName>.Fail(
                Error.Validation("Name cannot be empty", "Name")
            );

        if (value.Length < 3)
            return Result<CustomerName>.Fail(
                Error.Validation("Name must have at least 3 characters", "Name")
            );

        if (value.Length > 150)
            return Result<CustomerName>.Fail(
                Error.Validation("Name must have at most 150 characters", "Name")
            );

        return Result<CustomerName>.Success(new CustomerName(value));
    }

    private static string Normalize(string input)
    {
        var trimmed = input.Trim();

        return Regex.Replace(trimmed, @"\s+", " ");
    }

    public override string ToString() => Value;
}
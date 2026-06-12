using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Employee.Domain.ValueObjects;

public sealed class EmployeeName
{
    public string Value { get; }

    private EmployeeName(string value)
    {
        Value = value;
    }

    public static Result<EmployeeName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<EmployeeName>.Fail(
                            Error.Validation("Name cannot be empty", "Name")
                        );

        return Result<EmployeeName>.Success(new EmployeeName(name.Trim()));
    }

    public override string ToString() => Value;
}
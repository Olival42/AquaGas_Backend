namespace AquaGas.Api.Modules.Employee.Domain.ValueObjects;

public sealed class EmployeeName
{
    public string Value { get; }

    private EmployeeName(string value)
    {
        Value = value;
    }

    public static EmployeeName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty");

        return new EmployeeName(name.Trim());
    }

    public override string ToString() => Value;
}
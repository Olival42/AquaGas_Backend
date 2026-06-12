using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Domain.ValueObjects;

public sealed class ProductName : IEquatable<ProductName>
{
    public string Value { get; }

    private ProductName(string value)
    {
        Value = value;
    }

    public static Result<ProductName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ProductName>.Fail(
                Error.Validation("Product name is required", "Name")
            );

        name = name.Trim();

        if (name.Length < 3)
            return Result<ProductName>.Fail(
                Error.Validation("Product name must have at least 3 characters", "Name")
            );

        return Result<ProductName>.Success(new ProductName(name));
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
        => obj is ProductName other && Equals(other);

    public bool Equals(ProductName? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static bool operator ==(ProductName? left, ProductName? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ProductName? left, ProductName? right)
        => !(left == right);

}
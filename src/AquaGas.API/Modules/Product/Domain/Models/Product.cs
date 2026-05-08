using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Utils;

namespace AquaGas.Api.Modules.Product.Domain.Models;

public sealed class Product
{
    public Guid Id { get; private set; }
    public ProductName Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public TypeProduct Type { get; private set; }
    public Price Price { get; private set; } = null!;
    public StockQuantity Quantity { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Product() { }

    public Product(
        ProductName name,
        TypeProduct type,
        Price price,
        StockQuantity quantity)
    {
        Id = Guid.NewGuid();
        Name = name;
        NormalizedName = StringNormalizer.Normalize(name.Value);
        Type = type;
        Price = price;
        Quantity = quantity;
        IsActive = true;
    }

    public void Update(
        ProductName? name,
        TypeProduct? type,
        Price? price,
        StockQuantity? quantity)
    {
        if (name is not null)
        {
            Name = name;
            NormalizedName = StringNormalizer.Normalize(name.Value);
        }

        if (type is not null)
            Type = type.Value;

        if (price is not null)
            Price = price;

        if (quantity is not null)
            Quantity = quantity;
    }

    public Result IncreaseStock(int amount)
    {
        var result = Quantity.Increase(amount);
        if (result.IsFailure) return result;

        Quantity = result.Value!;
        return Result.Success();
    }

    public Result DecreaseStock(int amount)
    {
        var result = Quantity.Decrease(amount);
        if (result.IsFailure) return result;

        Quantity = result.Value!;
        return Result.Success();
    }

    public void Reactivate() => IsActive = true;

    public Result Deactivate()
    {
        if (Quantity.Value > 0)
            return Result.Fail(Error.Conflict("Product still in stock"));

        IsActive = false;
        return Result.Success();
    }
}
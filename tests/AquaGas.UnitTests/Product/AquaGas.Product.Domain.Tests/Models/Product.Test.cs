using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.ValueObjects;
using Xunit;

public class ProductTests
{
    [Fact]
    public void Constructor_Should_Create_Product_With_Valid_Values()
    {
        var name = ProductName.Create("Água Mineral").Value!;
        var price = Price.Create(10.50m).Value!;
        var quantity = StockQuantity.Create(20).Value!;

        var product = new ProductEntity(
            name,
            TypeProduct.Water,
            price,
            quantity
        );

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(name, product.Name);
        Assert.Equal("AGUA MINERAL", product.NormalizedName);
        Assert.Equal(TypeProduct.Water, product.Type);
        Assert.Equal(price, product.Price);
        Assert.Equal(quantity, product.Quantity);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Update_Should_Update_All_Provided_Fields()
    {
        var product = CreateProduct();

        var newName = ProductName.Create("Gas Premium").Value!;
        var newPrice = Price.Create(25.99m).Value!;
        var newQuantity = StockQuantity.Create(50).Value!;

        product.Update(
            newName,
            TypeProduct.Gas,
            newPrice,
            newQuantity
        );

        Assert.Equal(newName, product.Name);
        Assert.Equal("GAS PREMIUM", product.NormalizedName);
        Assert.Equal(TypeProduct.Gas, product.Type);
        Assert.Equal(newPrice, product.Price);
        Assert.Equal(newQuantity, product.Quantity);
    }

    [Fact]
    public void Update_Should_Not_Change_Fields_When_Null()
    {
        var product = CreateProduct();

        var originalName = product.Name;
        var originalNormalizedName = product.NormalizedName;
        var originalType = product.Type;
        var originalPrice = product.Price;
        var originalQuantity = product.Quantity;

        product.Update(null, null, null, null);

        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalNormalizedName, product.NormalizedName);
        Assert.Equal(originalType, product.Type);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalQuantity, product.Quantity);
    }

    [Fact]
    public void Update_Should_Update_Only_Name_When_Other_Fields_Are_Null()
    {
        var product = CreateProduct();

        var newName = ProductName.Create("Água com Gás").Value!;

        var originalType = product.Type;
        var originalPrice = product.Price;
        var originalQuantity = product.Quantity;

        product.Update(newName, null, null, null);

        Assert.Equal(newName, product.Name);
        Assert.Equal("AGUA COM GAS", product.NormalizedName);
        Assert.Equal(originalType, product.Type);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalQuantity, product.Quantity);
    }

    [Fact]
    public void IncreaseStock_Should_Increase_Quantity()
    {
        var product = CreateProduct(quantity: 10);

        var result = product.IncreaseStock(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, product.Quantity.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void IncreaseStock_Should_Fail_When_Amount_Is_Invalid(int amount)
    {
        var product = CreateProduct(quantity: 10);

        var result = product.IncreaseStock(amount);

        Assert.True(result.IsFailure);
        Assert.Equal(10, product.Quantity.Value);
    }

    [Fact]
    public void DecreaseStock_Should_Decrease_Quantity()
    {
        var product = CreateProduct(quantity: 20);

        var result = product.DecreaseStock(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, product.Quantity.Value);
    }

    [Fact]
    public void DecreaseStock_Should_Set_Quantity_To_Zero_When_Amount_Equals_Stock()
    {
        var product = CreateProduct(quantity: 10);

        var result = product.DecreaseStock(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, product.Quantity.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-20)]
    public void DecreaseStock_Should_Fail_When_Amount_Is_Invalid(int amount)
    {
        var product = CreateProduct(quantity: 10);

        var result = product.DecreaseStock(amount);

        Assert.True(result.IsFailure);
        Assert.Equal(10, product.Quantity.Value);
    }

    [Fact]
    public void DecreaseStock_Should_Fail_When_Stock_Is_Insufficient()
    {
        var product = CreateProduct(quantity: 5);

        var result = product.DecreaseStock(10);

        Assert.True(result.IsFailure);
        Assert.Equal(5, product.Quantity.Value);
    }

    [Fact]
    public void Reactivate_Should_Set_IsActive_To_True()
    {
        var product = CreateProduct(quantity: 0);

        product.Deactivate();

        product.Reactivate();

        Assert.True(product.IsActive);
    }

    [Fact]
    public void Deactivate_Should_Deactivate_Product_When_Stock_Is_Zero()
    {
        var product = CreateProduct(quantity: 0);

        var result = product.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(product.IsActive);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_Product_Has_Stock()
    {
        var product = CreateProduct(quantity: 10);

        var result = product.Deactivate();

        Assert.True(result.IsFailure);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Multiple_Stock_Operations_Should_Maintain_Correct_State()
    {
        var product = CreateProduct(quantity: 100);

        product.DecreaseStock(30);
        product.IncreaseStock(20);
        product.DecreaseStock(10);

        Assert.Equal(80, product.Quantity.Value);
    }

    [Fact]
    public void Update_Should_Keep_NormalizedName_Uppercase_And_Without_Accents()
    {
        var product = CreateProduct();

        var newName = ProductName.Create("Água São João").Value!;

        product.Update(newName, null, null, null);

        Assert.Equal("AGUA SAO JOAO", product.NormalizedName);
    }

    private static ProductEntity CreateProduct(
        string name = "Água Mineral",
        TypeProduct type = TypeProduct.Water,
        decimal price = 10m,
        int quantity = 10)
    {
        return new ProductEntity(
            ProductName.Create(name).Value!,
            type,
            Price.Create(price).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }

    [Fact]
    public void Reactivate_Should_Keep_Product_Active_When_Already_Active()
    {
        var product = CreateProduct();

        product.Reactivate();

        Assert.True(product.IsActive);
    }

    [Fact]
    public void Deactivate_Should_Not_Change_Quantity()
    {
        var product = CreateProduct(quantity: 0);

        product.Deactivate();

        Assert.Equal(0, product.Quantity.Value);
    }

    [Fact]
    public void IncreaseStock_Should_Not_Change_Name_Or_Price()
    {
        var product = CreateProduct();

        var originalName = product.Name;
        var originalPrice = product.Price;

        product.IncreaseStock(10);

        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalPrice, product.Price);
    }

    [Fact]
    public void DecreaseStock_Should_Not_Change_Name_Or_Price()
    {
        var product = CreateProduct(quantity: 20);

        var originalName = product.Name;
        var originalPrice = product.Price;

        product.DecreaseStock(5);

        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalPrice, product.Price);
    }

    [Fact]
    public void Update_Should_Update_Only_Type()
    {
        var product = CreateProduct();

        product.Update(
            null,
            TypeProduct.Gas,
            null,
            null
        );

        Assert.Equal(TypeProduct.Gas, product.Type);
    }

    [Fact]
    public void Update_Should_Update_Only_Price()
    {
        var product = CreateProduct();

        var newPrice = Price.Create(99.99m).Value!;

        product.Update(
            null,
            null,
            newPrice,
            null
        );

        Assert.Equal(99.99m, product.Price.Value);
    }

    [Fact]
    public void Update_Should_Update_Only_Quantity()
    {
        var product = CreateProduct();

        var quantity = StockQuantity.Create(500).Value!;

        product.Update(
            null,
            null,
            null,
            quantity
        );

        Assert.Equal(500, product.Quantity.Value);
    }

    [Fact]
    public void Constructor_Should_Generate_Different_Ids_For_Different_Products()
    {
        var product1 = CreateProduct();
        var product2 = CreateProduct();

        Assert.NotEqual(product1.Id, product2.Id);
    }

    [Fact]
    public void IncreaseStock_Should_Work_With_Large_Values()
    {
        var product = CreateProduct(quantity: 1000000);

        var result = product.IncreaseStock(500000);

        Assert.True(result.IsSuccess);
        Assert.Equal(1500000, product.Quantity.Value);
    }

    [Fact]
    public void DecreaseStock_Should_Work_With_Large_Values()
    {
        var product = CreateProduct(quantity: 1000000);

        var result = product.DecreaseStock(999999);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, product.Quantity.Value);
    }
}
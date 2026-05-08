using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Modules.Product.Application.Mappings;
using Moq;
using Xunit;

public class GetAllProductsTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly GetAllProducts _useCase;

    public GetAllProductsTests()
    {
        ProductMapping.Register();

        _repositoryMock = new Mock<IProductRepository>();

        _useCase = new GetAllProducts(
            _repositoryMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Only_Active_Products()
    {
        var activeProduct1 = CreateProduct("Agua Crystal");
        var activeProduct2 = CreateProduct("Gas Premium", TypeProduct.Gas);

        var inactiveProduct = CreateProduct(
            name: "Product inactive",
            quantity: 0
        );
        inactiveProduct.Deactivate();

        var products = new List<ProductEntity>
        {
            activeProduct1,
            activeProduct2,
            inactiveProduct
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(products);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        Assert.DoesNotContain(
            result.Value,
            p => p.Name == "Inactive Product"
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Empty_List_When_No_Products_Exist()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Execute_Should_Return_Empty_List_When_All_Products_Are_Inactive()
    {
        var product1 = CreateProduct(
        name: "Product 1",
        quantity: 0
    );

        var product2 = CreateProduct(
            name: "Product 2",
            quantity: 0
        );

        product1.Deactivate();
        product2.Deactivate();

        var products = new List<ProductEntity>
        {
            product1,
            product2
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(products);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Execute_Should_Map_Product_Response_Correctly()
    {
        var product = CreateProduct(
            name: "Gas Premium",
            type: TypeProduct.Gas,
            price: 150.75m,
            quantity: 20
        );

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity> { product });

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);

        var response = result.Value!.First();

        Assert.Equal(product.Id, response.Id);
        Assert.Equal("Gas Premium", response.Name);
        Assert.Equal(TypeProduct.Gas, response.Type);
        Assert.Equal(150.75m, response.Price);
        Assert.Equal(20, response.Quantity);
    }

    [Fact]
    public async Task Execute_Should_Call_GetAllAsync_Once()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>());

        await _useCase.Execute();

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Return_All_Active_Products()
    {
        var products = new List<ProductEntity>
        {
            CreateProduct("Product 1"),
            CreateProduct("Product 2"),
            CreateProduct("Product 3")
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(products);

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
    }

    [Fact]
    public async Task Execute_Should_Throw_When_Repository_Throws_Exception()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("database error"));

        await Assert.ThrowsAsync<Exception>(
            () => _useCase.Execute()
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Modify_Product_Entities()
    {
        var product = CreateProduct(
            name: "Original Product",
            type: TypeProduct.Water,
            price: 50,
            quantity: 10
        );

        var originalName = product.Name.Value;
        var originalPrice = product.Price.Value;
        var originalQuantity = product.Quantity.Value;
        var originalType = product.Type;

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity> { product });

        await _useCase.Execute();

        Assert.Equal(originalName, product.Name.Value);
        Assert.Equal(originalPrice, product.Price.Value);
        Assert.Equal(originalQuantity, product.Quantity.Value);
        Assert.Equal(originalType, product.Type);
    }

    [Fact]
    public async Task Execute_Should_Return_Response_List_Not_Null()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>());

        var result = await _useCase.Execute();

        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Execute_Should_Not_Return_Inactive_Products_When_Mixed_List()
    {
        var active = CreateProduct("Active");

        var inactive = CreateProduct(
            name: "Product 1",
            quantity: 0
        );

        inactive.Deactivate();

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>
            {
                active,
                inactive
            });

        var result = await _useCase.Execute();

        Assert.Single(result.Value!);

        Assert.Contains(
            result.Value!,
            x => x.Name == "Active"
        );
    }

    [Fact]
    public async Task Execute_Should_Preserve_Order_From_Repository()
    {
        var product1 = CreateProduct("Product A");
        var product2 = CreateProduct("Product B");
        var product3 = CreateProduct("Product C");

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>
            {
            product1,
            product2,
            product3
            });

        var result = await _useCase.Execute();

        Assert.Equal("Product A", result.Value![0].Name);
        Assert.Equal("Product B", result.Value![1].Name);
        Assert.Equal("Product C", result.Value![2].Name);
    }

    [Fact]
    public async Task Execute_Should_Filter_Inactive_Products_In_The_Middle_Of_List()
    {
        var product1 = CreateProduct("Product 1");

        var inactive = CreateProduct(
            name: "Product 1",
            quantity: 0
        );
        inactive.Deactivate();

        var product2 = CreateProduct("Product 2");

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>
            {
            product1,
            inactive,
            product2
            });

        var result = await _useCase.Execute();

        Assert.Equal(2, result.Value!.Count);

        Assert.DoesNotContain(
            result.Value!,
            x => x.Name == "Inactive"
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Success_Even_When_All_Products_Are_Filtered()
    {
        var inactive1 = CreateProduct(
            name: "Product 1",
            quantity: 0
        );

        var inactive2 = CreateProduct(
            name: "Product 2",
            quantity: 0
        );

        inactive1.Deactivate();
        inactive2.Deactivate();

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>
            {
            inactive1,
            inactive2
            });

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Execute_Should_Return_Different_Product_Ids()
    {
        var product1 = CreateProduct("Product 1");
        var product2 = CreateProduct("Product 2");

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity>
            {
            product1,
            product2
            });

        var result = await _useCase.Execute();

        Assert.NotEqual(
            result.Value![0].Id,
            result.Value[1].Id
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Product_With_Zero_Quantity()
    {
        var product = CreateProduct(
            name: "Water",
            quantity: 0
        );

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity> { product });

        var result = await _useCase.Execute();

        Assert.Single(result.Value!);
        Assert.Equal(0, result.Value![0].Quantity);
    }

    [Fact]
    public async Task Execute_Should_Return_Product_With_High_Price()
    {
        var product = CreateProduct(
            name: "Premium Gas",
            price: 99999.99m
        );

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ProductEntity> { product });

        var result = await _useCase.Execute();

        Assert.Equal(99999.99m, result.Value![0].Price);
    }

    private static ProductEntity CreateProduct(
        string name,
        TypeProduct type = TypeProduct.Water,
        decimal price = 10,
        int quantity = 5)
    {
        return new ProductEntity(
            ProductName.Create(name).Value!,
            type,
            Price.Create(price).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }
}
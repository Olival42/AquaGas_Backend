using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.API.Modules.Product.Application.Mappings;
using Moq;
using Xunit;

public class GetByProductIdTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly GetByProductId _useCase;

    public GetByProductIdTests()
    {
        ProductMapping.Register();

        _repositoryMock = new Mock<IProductRepository>();

        _useCase = new GetByProductId(
            _repositoryMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Product_When_Product_Exists()
    {
        var product = CreateProduct();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(product.Id, result.Value!.Id);
        Assert.Equal(product.Name.Value, result.Value.Name);
        Assert.Equal(product.Type, result.Value.Type);
        Assert.Equal(product.Price.Value, result.Value.Price);
        Assert.Equal(product.Quantity.Value, result.Value.Quantity);
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_Product_Does_Not_Exist()
    {
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(id);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            e => e.Message == "Product not found"
        );
    }

    [Fact]
    public async Task Execute_Should_Call_GetByIdAsync_Once()
    {
        var product = CreateProduct();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(product.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Correct_ProductResponse_Data()
    {
        var product = CreateProduct(
            name: "Gas Premium",
            type: TypeProduct.Gas,
            price: 150.75m,
            quantity: 25
        );

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);

        var response = result.Value!;

        Assert.Equal("Gas Premium", response.Name);
        Assert.Equal(TypeProduct.Gas, response.Type);
        Assert.Equal(150.75m, response.Price);
        Assert.Equal(25, response.Quantity);
    }

    [Fact]
    public async Task Execute_Should_Return_Inactive_Product_When_Product_Is_Inactive()
    {
        var product = CreateProduct();

        product.Deactivate();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Execute_Should_Throw_When_Repository_Throws_Exception()
    {
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ThrowsAsync(new Exception("database error"));

        await Assert.ThrowsAsync<Exception>(
            () => _useCase.Execute(id)
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Response_With_Same_Id_As_Product()
    {
        var product = CreateProduct();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(product.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Execute_Should_Not_Return_Null_Response_When_Product_Exists()
    {
        var product = CreateProduct();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Execute_Should_Return_Only_One_Error_When_Product_Does_Not_Exist()
    {
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(id);

        Assert.True(result.IsFailure);

        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task Execute_Should_Return_NotFound_Error_Code_When_Product_Does_Not_Exist()
    {
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(id);

        Assert.True(result.IsFailure);

        Assert.Equal(
            "NOT_FOUND",
            result.Errors.First().Code
        );
    }

    [Fact]
    public async Task Execute_Should_Handle_Product_With_Zero_Quantity()
    {
        var product = CreateProduct(quantity: 0);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(0, result.Value!.Quantity);
    }

    [Fact]
    public async Task Execute_Should_Handle_Product_With_High_Price()
    {
        var product = CreateProduct(price: 999999.99m);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(999999.99m, result.Value!.Price);
    }

    [Fact]
    public async Task Execute_Should_Not_Modify_Product_State()
    {
        var product = CreateProduct(
            name: "Produto Original",
            type: TypeProduct.Water,
            price: 50,
            quantity: 10
        );

        var originalName = product.Name.Value;
        var originalPrice = product.Price.Value;
        var originalQuantity = product.Quantity.Value;
        var originalType = product.Type;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        Assert.Equal(originalName, product.Name.Value);
        Assert.Equal(originalPrice, product.Price.Value);
        Assert.Equal(originalQuantity, product.Quantity.Value);
        Assert.Equal(originalType, product.Type);
    }

    [Fact]
    public async Task Execute_Should_Not_Call_Any_Other_Repository_Methods()
    {
        var product = CreateProduct();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(product.Id),
            Times.Once
        );

        _repositoryMock.VerifyNoOtherCalls();
    }

    private static ProductEntity CreateProduct(
        string name = "Agua Crystal",
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
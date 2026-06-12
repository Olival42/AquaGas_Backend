using AquaGas.Auth.Application.Services;
using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.UseCases;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Product.Application.Mappings;
using AquaGas.Shared.Domain.Enums;
using Mapster;
using Moq;
using Xunit;
using AquaGas.Application.Services;
using AquaGas.Shared.Domain.ValueObjects;

public class UpdateProductTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserContextService> _userContextService = new();

    private readonly UpdateProduct _useCase;

    public UpdateProductTests()
    {
        ProductMapping.Register();

        _useCase = new UpdateProduct(
            _productRepository.Object,
            _auditLogService.Object,
            _userContextService.Object
        );
    }

    [Fact]
    public async Task Should_Update_Product_Name()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _productRepository
            .Setup(x => x.AnyByNameAsync("New Water", product.Id))
            .ReturnsAsync(false);

        var input = new UpdateProductInput
        {
            Name = "New Water"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Water", result.Value!.Name);

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Update_Product_Price()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Price = 25
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value!.Price);
    }

    [Fact]
    public async Task Should_Update_Product_Type_When_Stock_Is_Zero()
    {
        var product = CreateProduct(quantity: 0);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(TypeProduct.Gas, result.Value!.Type);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Product_Not_Found()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProductEntity?)null);

        var input = new UpdateProductInput
        {
            Name = "Updated"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Name_Already_Exists()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _productRepository
            .Setup(x => x.AnyByNameAsync("Existing Product", product.Id))
            .ReturnsAsync(true);

        var input = new UpdateProductInput
        {
            Name = "Existing Product"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Product with this name already exists",
            result.Errors.First().Message
        );

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Failure_When_Changing_Type_With_Stock()
    {
        var product = CreateProduct(quantity: 10);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Cannot change product type while stock exists",
            result.Errors.First().Message
        );

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Failure_When_UserId_Is_Invalid()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                Error.Unauthorized("Unauthorized")
            ));

        var input = new UpdateProductInput
        {
            Name = "Updated"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);

        _productRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Failure_When_UserName_Is_Invalid()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(
                Error.Unauthorized("Unauthorized")
            ));

        var input = new UpdateProductInput
        {
            Name = "Updated"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);

        _productRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Failure_When_Validation_Fails()
    {
        var input = new UpdateProductInput();

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);

        _productRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Log_Audit_When_Product_Is_Updated()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Price = 50
        };

        await _useCase.Execute(input, product.Id);

        _auditLogService.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "admin",
                AuditAction.UPDATE,
                "Product",
                product.Id,
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Update_Multiple_Fields()
    {
        var product = CreateProduct(quantity: 0);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _productRepository
            .Setup(x => x.AnyByNameAsync("Updated Product", product.Id))
            .ReturnsAsync(false);

        var input = new UpdateProductInput
        {
            Name = "Updated Product",
            Price = 99,
            Type = "Gas"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal("Updated Product", result.Value!.Name);
        Assert.Equal(99, result.Value.Price);
        Assert.Equal(TypeProduct.Gas, result.Value.Type);
    }

    [Fact]
    public async Task Should_Not_Call_AnyByName_When_Name_Is_Null()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Price = 20
        };

        await _useCase.Execute(input, product.Id);

        _productRepository.Verify(
            x => x.AnyByNameAsync(It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Not_Log_Audit_When_Update_Fails()
    {
        var product = CreateProduct(quantity: 10);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsFailure);

        _auditLogService.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Not_SaveChanges_When_Update_Fails()
    {
        var product = CreateProduct(quantity: 10);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Type = "Gas"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsFailure);

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Update_NormalizedName_When_Name_Changes()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _productRepository
            .Setup(x => x.AnyByNameAsync("Água Premium", product.Id))
            .ReturnsAsync(false);

        var input = new UpdateProductInput
        {
            Name = "Água Premium"
        };

        await _useCase.Execute(input, product.Id);

        Assert.Equal("AGUA PREMIUM", product.NormalizedName);
    }

    [Fact]
    public async Task Should_Not_Change_Unspecified_Fields()
    {
        var product = CreateProduct();

        var originalName = product.Name.Value;
        var originalType = product.Type;

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Price = 99
        };

        await _useCase.Execute(input, product.Id);

        Assert.Equal(originalName, product.Name.Value);
        Assert.Equal(originalType, product.Type);
        Assert.Equal(99, product.Price.Value);
    }

    [Fact]
    public async Task Should_Keep_Product_Active_After_Update()
    {
        var product = CreateProduct();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateProductInput
        {
            Price = 20
        };

        await _useCase.Execute(input, product.Id);

        Assert.True(product.IsActive);
    }

    private static ProductEntity CreateProduct(int quantity = 10)
    {
        var name = ProductName.Create("Water 20L").Value!;
        var price = Price.Create(10).Value!;
        var stock = StockQuantity.Create(quantity).Value!;

        return new ProductEntity(
            name,
            TypeProduct.Water,
            price,
            stock
        );
    }
}
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Product.Application.Mappings;
using AquaGas.API.Shared.Application.Services;
using Moq;
using Xunit;

public class RegisterProductTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly RegisterProduct _useCase;

    public RegisterProductTests()
    {
        ProductMapping.Register();

        _repositoryMock = new Mock<IProductRepository>();
        _auditMock = new Mock<IAuditLogService>();
        _userContextMock = new Mock<IUserContextService>();

        _useCase = new RegisterProduct(
            _repositoryMock.Object,
            _auditMock.Object,
            _userContextMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Register_Product_When_Data_Is_Valid()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ProductEntity?)null);

        ProductEntity? addedProduct = null;

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductEntity>()))
            .Callback<ProductEntity>(p => addedProduct = p)
            .Returns(Task.CompletedTask);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Agua Crystal", result.Value!.Name);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductEntity>()),
            Times.Once
        );

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );

        _auditMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "admin",
                AquaGas.API.Shared.Domain.Enums.AuditAction.CREATE,
                "Product",
                It.IsAny<Guid>(),
                null,
                It.IsAny<object>()
            ),
            Times.Once
        );

        Assert.NotNull(addedProduct);
        Assert.True(addedProduct!.IsActive);
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_Validation_Fails()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "",
            Price = 0,
            Quantity = -1
        };

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _repositoryMock.Verify(
            x => x.GetByNameAsync(It.IsAny<string>()),
            Times.Never
        );

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductEntity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserId_Fails()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua",
            Type = "Water",
            Price = 10,
            Quantity = 1
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(
                AquaGas.Api.Shared.Errors.Error.Unauthorized("Unauthorized")
            ));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _repositoryMock.Verify(
            x => x.GetByNameAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserName_Fails()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua",
            Type = "Water",
            Price = 10,
            Quantity = 1
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(
                AquaGas.Api.Shared.Errors.Error.Unauthorized("Unauthorized")
            ));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _repositoryMock.Verify(
            x => x.GetByNameAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_Active_Product_Already_Exists()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var existingProduct = CreateProduct();

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(existingProduct);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductEntity>()),
            Times.Never
        );

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );

        _auditMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AquaGas.API.Shared.Domain.Enums.AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Reactivate_Product_When_Product_Exists_And_Is_Inactive()
    {
        var input = new RegisterProductInput
        {
            Name = "Gas",
            Type = "Gas",
            Price = 120,
            Quantity = 10
        };

        var existingProduct = CreateProduct();

        existingProduct.Deactivate();

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(existingProduct);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);
        Assert.True(existingProduct.IsActive);
        Assert.Equal(120, existingProduct.Price.Value);
        Assert.Equal(10, existingProduct.Quantity.Value);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );

        _auditMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                "admin",
                AquaGas.API.Shared.Domain.Enums.AuditAction.UPDATE,
                "Product",
                existingProduct.Id,
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Save_When_AddAsync_Throws()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ProductEntity?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new Exception("database error"));

        await Assert.ThrowsAsync<Exception>(() => _useCase.Execute(input));

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Call_SaveChanges_When_Product_Already_Exists_And_Is_Active()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 10,
            Quantity = 5
        };

        var existingProduct = CreateProduct();

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(existingProduct);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Log_When_Validation_Fails()
    {
        var input = new RegisterProductInput
        {
            Name = "",
            Type = "",
            Price = 0,
            Quantity = -1
        };

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _auditMock.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AquaGas.API.Shared.Domain.Enums.AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Update_All_Product_Fields_When_Reactivating()
    {
        var inactiveProduct = CreateProduct(
            name: "Produto Antigo",
            type: TypeProduct.Water,
            price: 50,
            quantity: 0
        );

        inactiveProduct.Deactivate();

        var input = new RegisterProductInput
        {
            Name = "Gas Novo",
            Type = "Gas",
            Price = 120,
            Quantity = 15
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(inactiveProduct);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("Gas Novo", inactiveProduct.Name.Value);
        Assert.Equal(TypeProduct.Gas, inactiveProduct.Type);
        Assert.Equal(120, inactiveProduct.Price.Value);
        Assert.Equal(15, inactiveProduct.Quantity.Value);
        Assert.True(inactiveProduct.IsActive);
    }

    [Fact]
    public async Task Execute_Should_Return_ProductResponse_With_Correct_Data()
    {
        var input = new RegisterProductInput
        {
            Name = "Agua Crystal",
            Type = "Water",
            Price = 25.50m,
            Quantity = 30
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        Assert.Equal("Agua Crystal", result.Value!.Name);
        Assert.Equal(TypeProduct.Water, result.Value.Type);
        Assert.Equal(25.50m, result.Value.Price);
        Assert.Equal(30, result.Value.Quantity);
    }

    [Fact]
    public async Task Execute_Should_Not_Add_Product_When_Reactivating()
    {
        var inactiveProduct = CreateProduct();

        inactiveProduct.Deactivate();

        var input = new RegisterProductInput
        {
            Name = "Produto Teste",
            Type = "Water",
            Price = 20,
            Quantity = 5
        };

        _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _repositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(inactiveProduct);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductEntity>()),
            Times.Never
        );
    }

    private static ProductEntity CreateProduct(
        string name = "Produto Teste",
        TypeProduct type = TypeProduct.Water,
        decimal price = 10,
        int quantity = 0)
    {
        return new ProductEntity(
            ProductName.Create(name).Value!,
            type,
            Price.Create(price).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }
}
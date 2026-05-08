using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Product.Application.UseCases;
using AquaGas.Api.Modules.Product.Domain.Enums;
using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Modules.Product.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Product.Application.Mappings;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using Moq;
using Xunit;

public class DeactiveProductTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUserContextService> _userContextServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;

    private readonly DeactiveProduct _useCase;

    public DeactiveProductTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _userContextServiceMock = new Mock<IUserContextService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _useCase = new DeactiveProduct(
            _productRepositoryMock.Object,
            _userContextServiceMock.Object,
            _auditLogServiceMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_Deactivate_Product_When_Stock_Is_Zero()
    {
        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.False(product.IsActive);

        _productRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserId_Is_Invalid()
    {
        var error = Error.Unauthorized("Unauthorized");

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(error));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message == "Unauthorized"
        );

        _productRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_UserName_Is_Invalid()
    {
        var error = Error.Unauthorized("Unauthorized");

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Fail(error));

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message == "Unauthorized"
        );

        _productRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message == "Product not found"
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Failure_When_Product_Has_Stock()
    {
        var product = CreateProduct(quantity: 10);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message == "Product still in stock"
        );

        Assert.True(product.IsActive);

        _productRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Log_Audit_When_Product_Is_Deactivated()
    {
        var userId = Guid.NewGuid();

        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                userId,
                "admin",
                AuditAction.DEACTIVATE,
                "Product",
                product.Id,
                It.IsAny<object>(),
                It.IsAny<object>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Log_Audit_When_Deactivation_Fails()
    {
        var product = CreateProduct(quantity: 5);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _auditLogServiceMock.Verify(
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
    public async Task Execute_Should_Call_SaveChanges_Once_When_Successful()
    {
        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _productRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Call_SaveChanges_When_Deactivation_Fails()
    {
        var product = CreateProduct(quantity: 3);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _productRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_Should_Throw_When_Repository_Throws_Exception()
    {
        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("database error"));

        await Assert.ThrowsAsync<Exception>(
            () => _useCase.Execute(Guid.NewGuid())
        );
    }

    [Fact]
    public async Task Execute_Should_Return_Success_With_Null_Value()
    {
        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Execute_Should_Keep_Product_Inactive_After_Save()
    {
        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task Execute_Should_Not_Change_Product_Quantity()
    {
        var product = CreateProduct(quantity: 0);

        var originalQuantity = product.Quantity.Value;

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        Assert.Equal(originalQuantity, product.Quantity.Value);
    }

    [Fact]
    public async Task Execute_Should_Call_GetByIdAsync_Once()
    {
        var product = CreateProduct(quantity: 0);

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await _useCase.Execute(product.Id);

        _productRepositoryMock.Verify(
            x => x.GetByIdAsync(product.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_Not_Deactivate_Already_Inactive_Product()
    {
        var product = CreateProduct(quantity: 0);

        product.Deactivate();

        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(product.Id);

        Assert.True(result.IsSuccess);
        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task Execute_Should_Not_Call_Audit_When_Product_Not_Found()
    {
        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        await _useCase.Execute(Guid.NewGuid());

        _auditLogServiceMock.Verify(
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
    public async Task Execute_Should_Not_Call_SaveChanges_When_Product_Not_Found()
    {
        _userContextServiceMock
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextServiceMock
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        await _useCase.Execute(Guid.NewGuid());

        _productRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    private static Product CreateProduct(
        string name = "Water",
        TypeProduct type = TypeProduct.Water,
        decimal price = 10,
        int quantity = 0)
    {
        return new Product(
            ProductName.Create(name).Value!,
            type,
            Price.Create(price).Value!,
            StockQuantity.Create(quantity).Value!
        );
    }
}
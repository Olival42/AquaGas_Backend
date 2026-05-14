using AquaGas.Auth.Application.Services;
using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.UseCases;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Product.Application.Mappings;
using AquaGas.Product.Application.Repositories;
using AquaGas.Shared.Domain.Enums;
using Mapster;
using Moq;
using Xunit;
using AquaGas.Application.Services;
using AquaGas.Shared.Domain.ValueObjects;

public class UpdateStockTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserContextService> _userContextService = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();

    private readonly UpdateStock _useCase;

    public UpdateStockTests()
    {
        ProductMapping.Register();

        _useCase = new UpdateStock(
            _productRepository.Object,
            _auditLogService.Object,
            _userContextService.Object,
            _stockMovementRepository.Object
        );
    }

    [Fact]
    public async Task Should_Increase_Stock_When_Type_Is_Entry()
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

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(15, result.Value.Quantity);

        _stockMovementRepository.Verify(
            x => x.AddAsync(It.IsAny<StockMovement>()),
            Times.Once
        );

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );

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
    public async Task Should_Decrease_Stock_When_Type_Is_Exit()
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

        var input = new UpdateStockInput
        {
            Quantity = 4,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Quantity);

        _stockMovementRepository.Verify(
            x => x.AddAsync(It.IsAny<StockMovement>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found", result.Errors.First().Message);

        _stockMovementRepository.Verify(
            x => x.AddAsync(It.IsAny<StockMovement>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Return_Failure_When_Stock_Is_Insufficient()
    {
        var product = CreateProduct(quantity: 2);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateStockInput
        {
            Quantity = 10,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsFailure);
        var err = result.Errors.First();
        Assert.Equal("INSUFFICIENT_STOCK", err.Code);
        Assert.Contains("Water 20L", err.Message, StringComparison.Ordinal);
        Assert.Contains(product.Id.ToString(), err.Message, StringComparison.Ordinal);
        Assert.Contains("Available: 2", err.Message, StringComparison.Ordinal);
        Assert.Contains("requested: 10", err.Message, StringComparison.Ordinal);

        _stockMovementRepository.Verify(
            x => x.AddAsync(It.IsAny<StockMovement>()),
            Times.Never
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

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
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

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
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
        var input = new UpdateStockInput
        {
            Quantity = 0,
            StockMovementType = "",
            Reason = ""
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);

        _productRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Create_StockMovement_With_Correct_Data()
    {
        var product = CreateProduct(quantity: 10);
        var userId = Guid.NewGuid();

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(userId));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        StockMovement? capturedMovement = null;

        _stockMovementRepository
            .Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(x => capturedMovement = x)
            .Returns(Task.CompletedTask);

        var input = new UpdateStockInput
        {
            Quantity = 3,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        await _useCase.Execute(input, product.Id);

        Assert.NotNull(capturedMovement);
        Assert.Equal(product.Id, capturedMovement.ProductId);
        Assert.Equal(3, capturedMovement.Quantity);
        Assert.Equal("Restock", capturedMovement.Reason);
        Assert.Equal(userId, capturedMovement.CreatedBy);
        Assert.Equal(StockMovementType.Entry, capturedMovement.Type);
    }

    [Fact]
    public async Task Should_Not_Save_When_StockMovement_Fails()
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

        _stockMovementRepository
            .Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
            .ThrowsAsync(new Exception());

        var input = new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
        };

        await Assert.ThrowsAsync<Exception>(() =>
            _useCase.Execute(input, product.Id));

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Not_Log_Audit_When_Stock_Update_Fails()
    {
        var product = CreateProduct(quantity: 1);

        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("admin"));

        _productRepository
            .Setup(x => x.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var input = new UpdateStockInput
        {
            Quantity = 999,
            StockMovementType = "Exit",
            Reason = "Sale"
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
    public async Task Should_Not_Create_StockMovement_When_Validation_Fails()
    {
        var input = new UpdateStockInput
        {
            Quantity = -1,
            StockMovementType = "Entry",
            Reason = "Invalid"
        };

        var result = await _useCase.Execute(input, Guid.NewGuid());

        Assert.True(result.IsFailure);

        _stockMovementRepository.Verify(
            x => x.AddAsync(It.IsAny<StockMovement>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Should_Keep_Product_Active_After_Stock_Update()
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

        var input = new UpdateStockInput
        {
            Quantity = 2,
            StockMovementType = "Exit",
            Reason = "Sale"
        };

        var result = await _useCase.Execute(input, product.Id);

        Assert.True(result.IsSuccess);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Should_Update_Quantity_Correctly_After_Multiple_Operations()
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

        await _useCase.Execute(new UpdateStockInput
        {
            Quantity = 5,
            StockMovementType = "Entry",
            Reason = "Restock"
        }, product.Id);

        await _useCase.Execute(new UpdateStockInput
        {
            Quantity = 3,
            StockMovementType = "Exit",
            Reason = "Sale"
        }, product.Id);

        Assert.Equal(12, product.Quantity.Value);
    }

    private static Product CreateProduct(int quantity)
    {
        var name = ProductName.Create("Water 20L").Value!;
        var price = Price.Create(10).Value!;
        var stock = StockQuantity.Create(quantity).Value!;

        return new Product(
            name,
            TypeProduct.Water,
            price,
            stock
        );
    }
}
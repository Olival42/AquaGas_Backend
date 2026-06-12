using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Product.Application.Repositories;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Domain.Models;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.ValueObjects;
using Moq;
using Xunit;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Enums;
using AquaGas.Shared.Domain.Enums;

namespace AquaGas.Sale.Application.Tests.UseCases;

public class CancelSaleTests
{
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUserContextService> _userContextService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private readonly CancelSale _useCase;

    public CancelSaleTests()
    {
        _useCase = new CancelSale(
            _saleRepository.Object,
            _productRepository.Object,
            _stockMovementRepository.Object,
            _userContextService.Object,
            _auditLogService.Object);
    }

    [Fact]
    public async Task Should_Cancel_Sale_Successfully()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        var result =
            await _useCase.Execute(
                sale.Id,
                input);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value);

        Assert.Equal(
            sale.Id,
            result.Value!.SaleId);

        Assert.Equal(
            SaleStatus.Canceled,
            result.Value.Status);

        Assert.Equal(
            "Customer canceled",
            result.Value.Reason);

        Assert.Equal(
            SaleStatus.Canceled,
            sale.Status);

        Assert.Equal(
            "Customer canceled",
            sale.CancelReason);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Once);

        _auditLogService.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                "Sale",
                sale.Id,
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Sale_Does_Not_Exist()
    {
        SetupAuthenticatedUser();

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Domain.Models.Sale?)null);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        var result =
            await _useCase.Execute(
                Guid.NewGuid(),
                input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message == "Sale not found");
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Sale_Is_Already_Canceled()
    {
        SetupAuthenticatedUser();

        var sale = CreateSale(Guid.NewGuid());

        sale.Cancel("Already canceled");

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        var input = new CancelSaleInput
        {
            Reason = "New reason"
        };

        var result =
            await _useCase.Execute(
                sale.Id,
                input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message ==
                "Sale already canceled");
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Cancellation_Period_Expired()
    {
        SetupAuthenticatedUser();

        var sale = new Domain.Models.Sale(
            null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null,
            DateTime.UtcNow.AddDays(-2));

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        var result =
            await _useCase.Execute(
                sale.Id,
                input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message ==
                "Cancellation period expired");
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        SetupAuthenticatedUser();

        var productId = Guid.NewGuid();

        var sale = CreateSale(productId);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                productId,
                false))
            .ReturnsAsync((ProductEntity?)null);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        var result =
            await _useCase.Execute(
                sale.Id,
                input);

        Assert.True(result.IsFailure);

        Assert.Contains(
            result.Errors,
            x => x.Message.Contains(
                "not found"));
    }

    [Fact]
    public async Task Should_Return_Failure_When_UserId_Is_Invalid()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(
                Shared.Results.Result<Guid>.Fail(
                    Shared.Errors.Error.Unauthorized(
                        "Invalid user")));

        var result =
            await _useCase.Execute(
                Guid.NewGuid(),
                new CancelSaleInput
                {
                    Reason = "Cancel"
                });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Should_Return_Failure_When_UserName_Is_Invalid()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(
                Shared.Results.Result<Guid>.Success(
                    Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(
                Shared.Results.Result<string>.Fail(
                    Shared.Errors.Error.Unauthorized(
                        "Invalid username")));

        var result =
            await _useCase.Execute(
                Guid.NewGuid(),
                new CancelSaleInput
                {
                    Reason = "Cancel"
                });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Should_Increase_Product_Stock_When_Canceling_Sale()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var originalQuantity = product.Quantity.Value;

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        await _useCase.Execute(
            sale.Id,
            input);

        Assert.Equal(
            originalQuantity + 2,
            product.Quantity.Value);
    }

    [Fact]
    public async Task Should_Save_Product_Changes_When_Canceling_Sale()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        await _useCase.Execute(
            sale.Id,
            input);

        _productRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task Should_Save_Sale_Changes_When_Canceling_Sale()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        await _useCase.Execute(
            sale.Id,
            input);

        _saleRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task Should_Update_Sale_When_Canceling()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        await _useCase.Execute(
            sale.Id,
            input);

        _saleRepository.Verify(
            x => x.Update(sale),
            Times.Once);
    }

    [Fact]
    public async Task Should_Create_StockMovement_With_Correct_Data()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        StockMovement? capturedMovement = null;

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        _stockMovementRepository
            .Setup(x => x.AddAsync(
                It.IsAny<StockMovement>()))
            .Callback<StockMovement>(
                x => capturedMovement = x);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        await _useCase.Execute(
            sale.Id,
            input);

        Assert.NotNull(capturedMovement);

        Assert.Equal(
            product.Id,
            capturedMovement!.ProductId);

        Assert.Equal(
            StockMovementType.Entry,
            capturedMovement.Type);

        Assert.Equal(
            2,
            capturedMovement.Quantity);

        Assert.Equal(
            sale.Id,
            capturedMovement.ReferenceId);

        Assert.Contains(
            "Customer canceled",
            capturedMovement.Reason);
    }

    [Fact]
    public async Task Should_Not_Create_StockMovement_When_Sale_Not_Found()
    {
        SetupAuthenticatedUser();

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Domain.Models.Sale?)null);

        var result = await _useCase.Execute(
            Guid.NewGuid(),
            new CancelSaleInput
            {
                Reason = "Cancel"
            });

        Assert.True(result.IsFailure);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Log_Audit_When_Cancellation_Fails()
    {
        SetupAuthenticatedUser();

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Domain.Models.Sale?)null);

        await _useCase.Execute(
            Guid.NewGuid(),
            new CancelSaleInput
            {
                Reason = "Cancel"
            });

        _auditLogService.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Handle_Multiple_Items_When_Canceling_Sale()
    {
        SetupAuthenticatedUser();

        var product1 = CreateProduct();
        var product2 = CreateProduct();

        var sale = new Domain.Models.Sale(
            null,
            Guid.NewGuid(),
            Price.Create(200).Value!,
            null);

        sale.AddItem(new SaleItem(
            sale.Id,
            product1.Id,
            StockQuantity.Create(2).Value!,
            Price.Create(100).Value!));

        sale.AddItem(new SaleItem(
            sale.Id,
            product2.Id,
            StockQuantity.Create(3).Value!,
            Price.Create(100).Value!));

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(
                sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product1.Id,
                false))
            .ReturnsAsync(product1);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product2.Id,
                false))
            .ReturnsAsync(product2);

        var input = new CancelSaleInput
        {
            Reason = "Customer canceled"
        };

        var result = await _useCase.Execute(
            sale.Id,
            input);

        Assert.True(result.IsSuccess);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Exactly(2));

        Assert.Equal(12, product1.Quantity.Value);
        Assert.Equal(13, product2.Quantity.Value);
    }

    [Fact]
    public async Task Should_Return_Correct_Response_Data()
    {
        SetupAuthenticatedUser();

        var product = CreateProduct();

        var sale = CreateSale(product.Id);

        _saleRepository
            .Setup(x => x.GetByIdWithItemsAsync(sale.Id))
            .ReturnsAsync(sale);

        _productRepository
            .Setup(x => x.GetByIdAsync(
                product.Id,
                false))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(
            sale.Id,
            new CancelSaleInput
            {
                Reason = "Customer canceled"
            });

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value);

        Assert.Equal(
            sale.Id,
            result.Value!.SaleId);

        Assert.Equal(
            SaleStatus.Canceled,
            result.Value.Status);

        Assert.Equal(
            "Customer canceled",
            result.Value.Reason);
    }

    private void SetupAuthenticatedUser()
    {
        _userContextService
            .Setup(x => x.GetUserId())
            .Returns(
                Shared.Results.Result<Guid>.Success(
                    Guid.NewGuid()));

        _userContextService
            .Setup(x => x.GetUserName())
            .Returns(
                Shared.Results.Result<string>.Success(
                    "admin"));
    }

    private static Domain.Models.Sale CreateSale(
        Guid productId)
    {
        var sale = new Domain.Models.Sale(
            null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null);

        var item = new SaleItem(
            sale.Id,
            productId,
            StockQuantity.Create(2).Value!,
            Price.Create(100).Value!);

        sale.AddItem(item);

        return sale;
    }

    private static ProductEntity CreateProduct()
    {
        return new ProductEntity(
            ProductName.Create("Water gallon").Value!,
            TypeProduct.Water,
            Price.Create(50).Value!,
            StockQuantity.Create(10).Value!);
    }
}
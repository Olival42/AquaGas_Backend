using Moq;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Application.Services;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Product.Domain.Models;
using AquaGas.Employee.Domain.Models;
using AquaGas.Customer.Domain.Models;
using AquaGas.Shared.Results;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Sale.Domain.Models;
using Microsoft.EntityFrameworkCore;
using AquaGas.Product.Application.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Product.Domain.Enums;

public class RegisterSaleTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();

    private readonly RegisterSale _useCase;

    public RegisterSaleTests()
    {
        _useCase = new RegisterSale(
            _saleRepo.Object,
            _productRepo.Object,
            _userContext.Object,
            _userRepo.Object,
            _employeeRepo.Object,
            _customerRepo.Object,
            _audit.Object,
            _stockMovementRepository.Object
        );
    }

    private static RegisterSaleInput CreateInput(Guid productId, Guid? customerId = null)
    {
        return new RegisterSaleInput
        {
            CustomerId = customerId,
            Discount = null,
            SaleItems = new List<SaleItemsInput>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1
                }
            }
        };
    }

    private static Product CreateProduct(Guid id)
    {
        var product = new Product(
            ProductName.Create("Prod").Value!,
            TypeProduct.Gas,
            Price.Create(100).Value!,
            StockQuantity.Create(10).Value!
        );

        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, id);

        return product;
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("Emp").Value!,
            Cpf.Create("71461516030").Value!,
            Email.Create("test@test.com").Value!,
            Phone.Create("4499999999").Value!
        );
    }

    private static Customer CreateCustomer()
    {
        return new Customer(
            CustomerName.Create("John").Value!,
            Document.Create("81322894043").Value!,
            Email.Create("test@test.com").Value!,
            Phone.Create("4499999999").Value!
        );
    }

    [Fact]
    public async Task Should_Create_Sale_Successfully()
    {
        var productId = Guid.NewGuid();
        var input = CreateInput(productId, Guid.NewGuid());

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateCustomer());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(100, result.Value.Total);
    }

    [Fact]
    public async Task Should_Fail_When_Product_Not_Found()
    {
        var productId = Guid.NewGuid();
        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("was not found"));
    }

    [Fact]
    public async Task Should_Fail_When_Insufficient_Stock()
    {
        var productId = Guid.NewGuid();
        var input = CreateInput(productId);

        SetupValidUserContext();

        var product = CreateProduct(productId);
        product.DecreaseStock(StockQuantity.Create(999).Value!.Value);

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Is_Employee_And_Uses_Discount()
    {
        var productId = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = null,
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
            {
                new() { ProductId = productId, Quantity = 1 }
            }
        };

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Employee));

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("Employees cannot apply discounts"));
    }

    [Fact]
    public async Task Should_Calculate_Discount_Correctly()
    {
        var productId = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = null,
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
            {
                new() { ProductId = productId, Quantity = 1 }
            }
        };

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(90, result.Value!.Total);
    }

    [Fact]
    public async Task Should_Fail_When_Customer_Not_Found()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = customerId,
            Discount = null,
            SaleItems = new List<SaleItemsInput>
        {
            new() { ProductId = productId, Quantity = 1 }
        }
        };

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _customerRepo.Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync((Customer?)null);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message == "Customer not found");
    }

    [Fact]
    public async Task Should_Fail_When_User_Not_Found()
    {
        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var input = CreateInput(Guid.NewGuid());

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Authenticated user was not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Not_Found()
    {
        var productId = Guid.NewGuid();

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var input = CreateInput(productId);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Employee linked to user was not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task Should_Never_Return_Negative_Total()
    {
        var productId = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = null,
            Discount = 10,
            SaleItems = new List<SaleItemsInput>
        {
            new() { ProductId = productId, Quantity = 1 }
        }
        };

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Total >= 0);
    }

    [Fact]
    public async Task Should_Update_All_Products_When_Sale_Is_Created()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        _productRepo.Verify(x => x.Update(It.IsAny<Product>()), Times.Once);
        _productRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Save_Sale_In_Database()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        _saleRepo.Verify(x => x.AddAsync(It.IsAny<Sale>()), Times.Once);
        _saleRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_On_Concurrency_Exception()
    {
        var productId = Guid.NewGuid();
        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User(UserName.Create("Admin").Value!, "Hash", Role.Manager, Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        _productRepo.Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("Stock concurrency conflict"));
    }

    [Fact]
    public async Task Should_Create_Stock_Movement_When_Sale_Is_Created()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Create_Stock_Movement_With_Correct_Data()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        var currentUserId = Guid.NewGuid();

        SetupValidUserContext(currentUserId);

        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(currentUserId));

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        StockMovement? capturedMovement = null;

        _stockMovementRepository
            .Setup(x => x.AddAsync(
                It.IsAny<StockMovement>()))
            .Callback<StockMovement>(
                x => capturedMovement = x);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        Assert.NotNull(capturedMovement);

        Assert.Equal(
            productId,
            capturedMovement!.ProductId);

        Assert.Equal(
            StockMovementType.Exit,
            capturedMovement.Type);

        Assert.Equal(
            1,
            capturedMovement.Quantity);

        Assert.Equal(
            "Sale completed",
            capturedMovement.Reason);

        Assert.Equal(
            currentUserId,
            capturedMovement.CreatedBy);

        Assert.Equal(
            result.Value!.Id,
            capturedMovement.ReferenceId);
    }

    [Fact]
    public async Task Should_Create_One_Stock_Movement_Per_Item()
    {
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = null,
            Discount = null,
            SaleItems = new List<SaleItemsInput>
        {
            new()
            {
                ProductId = productId1,
                Quantity = 1
            },
            new()
            {
                ProductId = productId2,
                Quantity = 2
            }
        }
        };

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId1))
            .ReturnsAsync(CreateProduct(productId1));

        _productRepo.Setup(x => x.GetByIdAsync(productId2))
            .ReturnsAsync(CreateProduct(productId2));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Not_Create_Stock_Movement_When_Product_Not_Found()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Create_Stock_Movement_When_Stock_Is_Insufficient()
    {
        var productId = Guid.NewGuid();

        var input = new RegisterSaleInput
        {
            CustomerId = null,
            Discount = null,
            SaleItems = new List<SaleItemsInput>
        {
            new()
            {
                ProductId = productId,
                Quantity = 999
            }
        }
        };

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsFailure);

        _stockMovementRepository.Verify(
            x => x.AddAsync(
                It.IsAny<StockMovement>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Decrease_Product_Stock_When_Sale_Is_Created()
    {
        var productId = Guid.NewGuid();

        var product = CreateProduct(productId);

        var initialQuantity = product.Quantity.Value;

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            initialQuantity - 1,
            product.Quantity.Value);
    }

    [Fact]
    public async Task Should_Log_Audit_When_Sale_Is_Created()
    {
        var productId = Guid.NewGuid();

        var input = CreateInput(productId);

        SetupValidUserContext();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new User(
                    UserName.Create("Admin").Value!,
                    "Hash",
                    Role.Manager,
                    Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(input);

        Assert.True(result.IsSuccess);

        _audit.Verify(
            x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                AuditAction.CREATE,
                "Sale",
                It.IsAny<Guid>(),
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    private void SetupValidUserContext(
        Guid? userId = null)
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(
                Result<Guid>.Success(
                    userId ?? Guid.NewGuid()));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("user"));

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));
    }
}

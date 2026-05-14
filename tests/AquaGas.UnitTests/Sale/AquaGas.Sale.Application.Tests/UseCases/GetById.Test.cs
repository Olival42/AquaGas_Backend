using Xunit;
using Moq;
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Sale.Domain.Models;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Employee.Domain.Models;
using AquaGas.Customer.Domain.Models;
using AquaGas.Customer.Domain.ValueObjects.Customer;

public class GetByIdTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private readonly GetById _useCase;

    public GetByIdTests()
    {
        _useCase = new GetById(
            _saleRepo.Object,
            _customerRepo.Object,
            _employeeRepo.Object,
            _productRepo.Object);
    }

    [Fact]
    public async Task Should_Return_Sale_Successfully()
    {
        var product = CreateProduct();
        var sale = CreateSale(withCustomer: true);

        var item = CreateItem(product.Id);
        sale.AddItem(item);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateCustomer());

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(item.ProductId, false))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Sale_Does_Not_Exist()
    {
        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Sale?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Sale not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync((Employee?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Employee not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task Should_Return_Sale_With_Null_Customer_When_Not_Found()
    {
        var sale = CreateSale(withCustomer: true);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync((Customer?)null);

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Customer);
    }

    [Fact]
    public async Task Should_Calculate_Subtotal_Correctly()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        var expected = sale.Items.Sum(x => x.TotalPrice.Value);

        Assert.Equal(expected, result.Value!.Subtotal);
    }

    [Fact]
    public async Task Should_Return_Discount_When_Present()
    {
        var sale = CreateSale();
        sale.UpdateDiscount(Discount.Create(15).Value!);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.Equal(15, result.Value!.Discount);
    }

    [Fact]
    public async Task Should_Set_Product_As_Unknown_When_Not_Found()
    {
        var product = CreateProduct();
        var sale = CreateSale();

        var item = CreateItem(product.Id);
        sale.AddItem(item);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(item.ProductId, false))
            .ReturnsAsync((Product?)null);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal("Unknown", result.Value!.Items[0].ProductName);
    }

    [Fact]
    public async Task Should_Return_Multiple_Items_Correctly()
    {
        var product = CreateProduct();
        var sale = CreateSale();

        var item1 = CreateItem(product.Id);
        var item2 = CreateItem(product.Id);

        sale.AddItem(item1);
        sale.AddItem(item2);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(product);

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task Should_Calculate_Total_Correctly()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.Equal(sale.Total.Value, result.Value!.Total);
    }

    [Fact]
    public async Task Should_Preserve_Customer_Data_When_Exists()
    {
        var sale = CreateSale(withCustomer: true);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateCustomer());

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.NotNull(result.Value!.Customer);
        Assert.Equal("John", result.Value.Customer!.Name);
    }

    [Fact]
    public async Task Should_Preserve_Sale_Date()
    {
        var sale = CreateSale();
        var originalDate = sale.Date;

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.Equal(originalDate, result.Value!.CreatedAt);
    }

    [Fact]
    public async Task Should_Reflect_Discount_In_Response()
    {
        var sale = CreateSale();
        sale.UpdateDiscount(Discount.Create(20).Value!);

        _saleRepo.Setup(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(sale);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute(Guid.NewGuid());

        Assert.Equal(20, result.Value!.Discount);
    }

    private static Sale CreateSale(bool withCustomer = true)
    {
        return new Sale(
            withCustomer ? Guid.NewGuid() : null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null);
    }

    private static SaleItem CreateItem(Guid productId)
    {
        return new SaleItem(
            Guid.NewGuid(),
            productId,
            StockQuantity.Create(1).Value!,
            Price.Create(100).Value!
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

    private static Employee CreateEmployee()
    {
        return new Employee(
            EmployeeName.Create("Emp").Value!,
            Cpf.Create("71461516030").Value!,
            Email.Create("test@test.com").Value!,
            Phone.Create("4499999999").Value!
        );
    }

    private static Product CreateProduct()
    {
        return new Product(
            ProductName.Create("Prod").Value!,
            TypeProduct.Gas,
            Price.Create(20).Value!,
            StockQuantity.Create(20).Value!
        );
    }
}
using AquaGas.Sale.Application.UseCases;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Sale.Domain.Models;
using Moq;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Customer.Domain.Models;
using AquaGas.Employee.Domain.Models;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Product.Domain.Enums;

public class GetAllSalesTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private readonly GetAllSales _useCase;

    public GetAllSalesTests()
    {
        _useCase = new GetAllSales(
            _saleRepo.Object,
            _customerRepo.Object,
            _employeeRepo.Object,
            _productRepo.Object);
    }

    [Fact]
    public async Task Should_Return_Sales_Successfully()
    {
        var sale = CreateSale(withCustomer: true);

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateCustomer());

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task Should_Handle_Sale_Without_Customer()
    {
        var sale = CreateSale(withCustomer: false);

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value![0].Customer);
    }

    [Fact]
    public async Task Should_Return_Unknown_When_Employee_Not_Found()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync((Employee?)null);

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.Equal("Unknown", result.Value![0].Employee.Name);
    }

    [Fact]
    public async Task Should_Calculate_Subtotal_And_Total()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        var saleResponse = result.Value![0];

        Assert.Equal(
            sale.Items.Sum(x => x.TotalPrice.Value),
            saleResponse.Subtotal);

        Assert.Equal(
            sale.Total.Value,
            saleResponse.Total);
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Sales_Exist()
    {
        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale>());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Should_Return_Discount_When_Sale_Has_Discount()
    {
        var sale = new Sale(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Price.Create(100).Value!,
            Discount.Create(10).Value! // 10%
        );

        sale.AddItem(CreateItem());

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.Equal(10, result.Value![0].Discount);
    }

    [Fact]
    public async Task Should_Handle_Multiple_Items_In_Sale()
    {
        var sale = CreateSale();

        sale.AddItem(CreateItem());
        sale.AddItem(CreateItem());

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.Equal(2, result.Value![0].Items.Count);
    }

    [Fact]
    public async Task Should_Calculate_Subtotal_With_Multiple_Items()
    {
        var sale = CreateSale();

        sale.AddItem(CreateItem());
        sale.AddItem(CreateItem());

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        var expectedSubtotal = sale.Items.Sum(x => x.TotalPrice.Value);

        Assert.Equal(expectedSubtotal, result.Value![0].Subtotal);
    }

    [Fact]
    public async Task Should_Set_Unknown_Employee_When_Null()
    {
        var sale = CreateSale();

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync((Employee?)null);

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.Equal("Unknown", result.Value![0].Employee.Name);
    }

    [Fact]
    public async Task Should_Keep_Customer_Null_When_Not_Found()
    {
        var sale = CreateSale(withCustomer: true);

        _saleRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sale> { sale });

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync((Customer?)null);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateEmployee());

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(CreateProduct());

        var result = await _useCase.Execute();

        Assert.Null(result.Value![0].Customer);
    }

    private static Sale CreateSale(bool withCustomer = true)
    {
        var sale = new Sale(
            withCustomer ? Guid.NewGuid() : null,
            Guid.NewGuid(),
            Price.Create(100).Value!,
            null);

        return sale;
    }

    private static SaleItem CreateItem()
    {
        return new SaleItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
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
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Report.Application.Configuration;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.UseCases;
using AquaGas.Sale.Domain.Models;
using SaleEntity = AquaGas.Sale.Domain.Models.Sale;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Report.Application.Tests.UseCases;

public sealed class GetSalesReportTests
{
    private readonly Mock<IUserContextService> _userContextService = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepository = new();
    private readonly Mock<IPlanRepository> _planRepository = new();
    private readonly Mock<IBillingRepository> _billingRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();

    private readonly GetSalesReport _useCase;

    public GetSalesReportTests()
    {
        _useCase = new GetSalesReport(
            _userContextService.Object,
            _saleRepository.Object,
            _deliveryRepository.Object,
            _planRepository.Object,
            _billingRepository.Object,
            _productRepository.Object,
            _employeeRepository.Object,
            _customerRepository.Object,
            new ReportSettings
            {
                MaxSalesReportIntervalDays = 366
            });
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_User_Is_Not_Manager()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Employee));

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Code == "FORBIDDEN");
    }

    [Fact]
    public async Task Should_Return_Spot_And_Contract_Sales_And_Ignore_NotDelivered_Deliveries()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();
        var employee = CreateEmployee();
        var customer = CreateCustomer("Joao da Silva");
        var sale = CreateSale(customer.Id, employee.Id, productId, 120m, false);
        var canceledSale = CreateSale(null, employee.Id, productId, 50m, true);
        var plan = CreatePlan(customer.Id, employee.Id, productId);
        var delivered = CreateDeliveredDelivery(plan.Id, 1, new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc));
        var pending = new Delivery(plan.Id, 2, new DateTime(2026, 05, 04, 10, 0, 0, DateTimeKind.Utc));
        var billing = new Billing(plan.Id, 1, new DateTime(2026, 05, 03, 0, 0, 0, DateTimeKind.Utc), Price.Create(180m).Value!);

        _saleRepository.Setup(x => x.GetByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([sale, canceledSale]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([delivered]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([billing]);

        _productRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([employee]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Select(x => x.Date).Should().BeInDescendingOrder();

        result.Value.Summary.TotalSpotSales.Should().Be(120m);
        result.Value.Summary.TotalContractSales.Should().Be(180m);
        result.Value.Summary.TotalRevenue.Should().Be(300m);
        result.Value.Summary.TotalSales.Should().Be(2);
        result.Value.Summary.CancelledSales.Should().Be(1);
        result.Value.Summary.AverageTicket.Should().Be(150m);

        result.Value.Items.Should().Contain(x => x.Type == "PLAN" && x.Status == "FINISHED");
        result.Value.Items.Should().Contain(x => x.Type == "SALE" && x.Status == "FINISHED");
        result.Value.Items.Should().Contain(x => x.Type == "SALE" && x.Status == "CANCELLED");
        result.Value.Items.Should().NotContain(x => x.Id == $"plan-{pending.Id}");
    }

    [Fact]
    public async Task Should_Fail_When_Billing_For_Delivered_Contract_Is_Missing()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();
        var employee = CreateEmployee();
        var customer = CreateCustomer("Maria");
        var plan = CreatePlan(customer.Id, employee.Id, productId);
        var delivered = CreateDeliveredDelivery(plan.Id, 1, new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc));

        _saleRepository.Setup(x => x.GetByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([delivered]);

        _planRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([employee]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([customer]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message.Contains("Billing"));
    }

    [Fact]
    public async Task Should_Fail_When_Period_Exceeds_Configured_Maximum()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var result = await _useCase.Execute(new SalesReportInput
        {
            Start = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2027, 06, 01, 0, 0, 0, DateTimeKind.Utc)
        });

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Field == "Period");
    }

    [Fact]
    public async Task Should_Return_Failure_When_User_Role_Cannot_Be_Resolved()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Fail(
                Shared.Errors.Error.Unauthorized("Unauthorized")));

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Code == "UNAUTHORIZED");
    }

    [Fact]
    public async Task Should_Return_Failure_When_Start_Is_Greater_Than_End()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var input = new SalesReportInput
        {
            Start = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Field == "Period" &&
            x.Message == "Start must be less than or equal to End");
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();
        var employee = CreateEmployee();

        var sale = CreateSale(
            null,
            employee.Id,
            productId,
            120m,
            false);

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([sale]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([employee]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().Contain(x =>
            x.Items.Any(i => i.ProductName == "Unknown"));
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Employee_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();

        var sale = CreateSale(
            null,
            Guid.NewGuid(),
            productId,
            120m,
            false);

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([sale]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Message.Contains($"Employee {sale.EmployeeId} not found"));
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Plan_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var delivered = CreateDeliveredDelivery(
            Guid.NewGuid(),
            1,
            new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc));

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([delivered]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Message.Contains($"Plan {delivered.PlanId} not found"));
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Plan_Employee_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();
        var customer = CreateCustomer("Maria");

        var plan = CreatePlan(
            customer.Id,
            Guid.NewGuid(),
            productId);

        var delivered = CreateDeliveredDelivery(
            plan.Id,
            1,
            new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc));

        var billing = new Billing(
            plan.Id,
            1,
            new DateTime(2026, 05, 03, 0, 0, 0, DateTimeKind.Utc),
            Price.Create(180m).Value!);

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([delivered]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([billing]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([customer]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Message.Contains($"Employee {plan.EmployeeId} not found"));
    }

    [Fact]
    public async Task Should_Set_Product_Name_As_Unknown_When_Product_Is_Not_Found_In_Contract_Item()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var employee = CreateEmployee();
        var customer = CreateCustomer("Carlos");

        var productId = Guid.NewGuid();

        var plan = CreatePlan(customer.Id, employee.Id, productId);

        var delivered = CreateDeliveredDelivery(
            plan.Id,
            1,
            new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc));

        var billing = new Billing(
            plan.Id,
            1,
            new DateTime(2026, 05, 03, 0, 0, 0, DateTimeKind.Utc),
            Price.Create(180m).Value!);

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([delivered]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([plan]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([billing]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([employee]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([customer]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();

        var item = result.Value!.Items.First();

        item.Items.Should().Contain(x =>
            x.ProductName == "Unknown");
    }

    [Fact]
    public async Task Should_Return_Zero_Average_Ticket_When_There_Are_No_Finished_Sales()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var productId = Guid.NewGuid();
        var employee = CreateEmployee();

        var canceledSale = CreateSale(
            null,
            employee.Id,
            productId,
            120m,
            true);

        _saleRepository.Setup(x => x.GetByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([canceledSale]);

        _deliveryRepository.Setup(x => x.GetDeliveredByPeriodAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _billingRepository.Setup(x => x.GetByPlanIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([employee]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();

        result.Value!.Summary.TotalRevenue.Should().Be(0m);
        result.Value.Summary.TotalSales.Should().Be(0);
        result.Value.Summary.AverageTicket.Should().Be(0m);
        result.Value.Summary.CancelledSales.Should().Be(1);
    }

    private static SalesReportInput CreateInput()
    {
        return new SalesReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 31, 23, 59, 59, DateTimeKind.Utc)
        };
    }

    private static ProductEntity CreateProduct(Guid productId)
    {
        var product = new ProductEntity(
            ProductName.Create("Botijao Gas 13kg").Value!,
            TypeProduct.Gas,
            Price.Create(120m).Value!,
            StockQuantity.Create(50).Value!);

        typeof(ProductEntity).GetProperty(nameof(ProductEntity.Id))!
            .SetValue(product, productId);

        return product;
    }

    private static EmployeeEntity CreateEmployee()
    {
        return new EmployeeEntity(
            EmployeeName.Create("Maria Souza").Value!,
            Cpf.Create("41553651014").Value!,
            Email.Create("maria@aquagas.com").Value!,
            Phone.Create("44999999998").Value!);
    }

    private static CustomerEntity CreateCustomer(string name)
    {
        return new CustomerEntity(
            CustomerName.Create(name).Value!,
            Document.Create("55964416004").Value!,
            Email.Create($"{name.Replace(" ", "").ToLowerInvariant()}@aquagas.com").Value!,
            Phone.Create("44999999999").Value!);
    }

    private static SaleEntity CreateSale(
        Guid? customerId,
        Guid employeeId,
        Guid productId,
        decimal total,
        bool canceled)
    {
        var sale = new SaleEntity(
            customerId,
            employeeId,
            Price.Create(total).Value!,
            null,
            new DateTime(2026, 05, 02, 10, 0, 0, DateTimeKind.Utc));

        sale.AddItem(new SaleItem(
            sale.Id,
            productId,
            StockQuantity.Create(1).Value!,
            Price.Create(total).Value!));

        if (canceled)
            sale.Cancel("Cliente cancelou");

        return sale;
    }

    private static PlanEntity CreatePlan(
        Guid customerId,
        Guid employeeId,
        Guid productId)
    {
        var plan = new PlanEntity(
            customerId,
            employeeId,
            PlanCycle.Monthly,
            Price.Create(180m).Value!,
            new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
            10,
            5);

        plan.AddItem(new PlanItem(
            plan.Id,
            productId,
            StockQuantity.Create(2).Value!));

        return plan;
    }

    private static Delivery CreateDeliveredDelivery(
        Guid planId,
        int period,
        DateTime deliveredAt)
    {
        var delivery = new Delivery(planId, period, deliveredAt.AddDays(-1));
        delivery.Complete();

        typeof(Delivery).GetProperty(nameof(Delivery.DeliveryDate))!
            .SetValue(delivery, deliveredAt);

        return delivery;
    }
}

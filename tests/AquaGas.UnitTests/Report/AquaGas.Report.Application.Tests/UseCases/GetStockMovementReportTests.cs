using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Application.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Models;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.UseCases;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using SaleEntity = AquaGas.Sale.Domain.Models.Sale;

namespace AquaGas.Report.Application.Tests.UseCases;

public sealed class GetStockMovementReportTests
{
    private readonly Mock<IUserContextService> _userContextService = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<IPlanRepository> _planRepository = new();

    private readonly GetStockMovementReport _useCase;

    public GetStockMovementReportTests()
    {
        _useCase = new GetStockMovementReport(
            _userContextService.Object,
            _stockMovementRepository.Object,
            _productRepository.Object,
            _employeeRepository.Object,
            _customerRepository.Object,
            _saleRepository.Object,
            _planRepository.Object);
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
    public async Task Should_Return_Ordered_Movements_With_Sale_And_Plan_References()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var saleCustomer = CreateCustomer("Joao Silva");
        var planCustomer = CreateCustomer("Maria Lima");
        var sale = CreateSale(saleCustomer.Id);
        var plan = CreatePlan(planCustomer.Id);

        var planDeliveryReferenceId = Guid.NewGuid();

        var movements = new List<StockMovement>
        {
            CreateMovement(productId, userId, StockMovementType.Exit, "Venda realizada", sale.Id, new DateTime(2026, 05, 02, 10, 0, 0, DateTimeKind.Utc)),
            CreateMovement(productId, userId, StockMovementType.Exit, "Plan delivery", planDeliveryReferenceId, new DateTime(2026, 05, 03, 10, 0, 0, DateTimeKind.Utc)),
            CreateMovement(productId, userId, StockMovementType.Entry, "Ajuste manual", null, new DateTime(2026, 05, 01, 10, 0, 0, DateTimeKind.Utc))
        };

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(movements);

        _productRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([CreateEmployee(userId)]);

        _saleRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([sale]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, PlanEntity>
            {
                [planDeliveryReferenceId] = plan
            });

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([saleCustomer, planCustomer]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Select(x => x.Date).Should().BeInDescendingOrder();

        var planItem = result.Value.Items[0];
        planItem.Reference!.Type.Should().Be("PLAN");
        planItem.Reference.Id.Should().Be(plan.Id);
        planItem.Customer!.Name.Should().Be("Maria Lima");

        var saleItem = result.Value.Items[1];
        saleItem.Reference!.Type.Should().Be("SALE");
        saleItem.Reference.Id.Should().Be(sale.Id);
        saleItem.Customer!.Name.Should().Be("Joao Silva");

        var manualItem = result.Value.Items[2];
        manualItem.Reference.Should().BeNull();
        manualItem.Customer.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Responsible_Employee_Is_Missing()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([
                CreateMovement(productId, userId, StockMovementType.Entry, "Ajuste", null, DateTime.UtcNow)
            ]);

        _productRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        _saleRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message.Contains("Employee responsible"));
    }

    [Fact]
    public async Task Should_Return_Failure_When_User_Role_Context_Is_Invalid()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Fail());

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Validation_Error_When_Start_Is_Greater_Than_End()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var input = new StockMovementReportInput
        {
            Start = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _useCase.Execute(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Code == "VALIDATION_ERROR" &&
            x.Message == "Start must be less than or equal to End");
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var movement = CreateMovement(
            productId,
            userId,
            StockMovementType.Entry,
            "Ajuste",
            null,
            DateTime.UtcNow);

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([movement]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateEmployee(userId)]);

        _saleRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x =>
            x.Message.Contains($"Product {productId} not found"));
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Movements_Are_Found()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        _saleRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Null_Customer_When_Customer_Is_Not_Found()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var sale = CreateSale(Guid.NewGuid());

        var movement = CreateMovement(
            productId,
            userId,
            StockMovementType.Exit,
            "Venda",
            sale.Id,
            DateTime.UtcNow);

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([movement]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateEmployee(userId)]);

        _saleRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([sale]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();

        var item = result.Value!.Items.First();

        item.Reference.Should().NotBeNull();
        item.Reference!.Type.Should().Be("SALE");

        item.Customer.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_Null_Reference_When_ReferenceId_Does_Not_Match_Sale_Or_Plan()
    {
        _userContextService.Setup(x => x.GetRole())
            .Returns(Shared.Results.Result<Role>.Success(Role.Manager));

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var movement = CreateMovement(
            productId,
            userId,
            StockMovementType.Exit,
            "Movimentacao",
            Guid.NewGuid(),
            DateTime.UtcNow);

        _stockMovementRepository.Setup(x => x.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync([movement]);

        _productRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateProduct(productId)]);

        _employeeRepository.Setup(x => x.GetByUserIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([CreateEmployee(userId)]);

        _saleRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByDeliveryReferenceIdsAsync(
                It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        _customerRepository.Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                false))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(CreateInput());

        result.IsSuccess.Should().BeTrue();

        var item = result.Value!.Items.First();

        item.Reference.Should().BeNull();
        item.Customer.Should().BeNull();
    }

    private static StockMovementReportInput CreateInput()
    {
        return new StockMovementReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 31, 23, 59, 59, DateTimeKind.Utc)
        };
    }

    private static CustomerEntity CreateCustomer(string name)
    {
        return new CustomerEntity(
            CustomerName.Create(name).Value!,
            Document.Create("55964416004").Value!,
            Email.Create($"{name.Replace(" ", "").ToLowerInvariant()}@email.com").Value!,
            Phone.Create("44999999999").Value!);
    }

    private static EmployeeEntity CreateEmployee(Guid userId)
    {
        var employee = new EmployeeEntity(
            EmployeeName.Create("Maria Souza").Value!,
            Cpf.Create("41553651014").Value!,
            Email.Create("maria@aquagas.com").Value!,
            Phone.Create("44999999998").Value!);

        employee.AssignUserId(userId);
        return employee;
    }

    private static AquaGas.Product.Domain.Models.Product CreateProduct(Guid id)
    {
        var product = new AquaGas.Product.Domain.Models.Product(
            ProductName.Create("Botijao Gas 13kg").Value!,
            TypeProduct.Gas,
            Price.Create(120m).Value!,
            StockQuantity.Create(30).Value!);

        typeof(AquaGas.Product.Domain.Models.Product).GetProperty(nameof(AquaGas.Product.Domain.Models.Product.Id))!
            .SetValue(product, id);

        return product;
    }

    private static SaleEntity CreateSale(Guid? customerId)
        => new(
            customerId,
            Guid.NewGuid(),
            Price.Create(240m).Value!,
            null,
            new DateTime(2026, 05, 02, 10, 0, 0, DateTimeKind.Utc));

    private static PlanEntity CreatePlan(Guid customerId)
        => new(
            customerId,
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(180m).Value!,
            new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
            10,
            5);

    private static StockMovement CreateMovement(
        Guid productId,
        Guid createdBy,
        StockMovementType type,
        string reason,
        Guid? referenceId,
        DateTime createdAt)
    {
        var movement = StockMovement.Create(
            productId,
            type,
            2,
            reason,
            createdBy,
            referenceId);

        typeof(StockMovement).GetProperty(nameof(StockMovement.CreatedAt))!
            .SetValue(movement, createdAt);

        return movement;
    }
}

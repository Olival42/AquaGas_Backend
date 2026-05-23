using AquaGas.Application.Services;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Repositories;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Moq;
using Xunit;
using FluentAssertions;
using AquaGas.Plan.Domain.Models;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Product.Domain.Enums;

namespace AquaGas.Plan.Application.Tests.UseCases;

public class RegisterPlanTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<IUserContextService> _userContext = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IPlanDateService> _dateService = new();
    private readonly Mock<IPlanCalculationService> _calcService = new();
    private readonly Mock<IPlanDeliveryService> _deliveryService = new();
    private readonly Mock<IPlanBillingService> _billingService = new();
    private readonly Mock<IPlanFactoryService> _factoryService = new();
    private readonly Mock<IAuditLogService> _audit = new();

    public RegisterPlanTests()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(_userId));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("Employee"));

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        _penaltyRepo.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContractPenalty>());

        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Billing>());
    }

    private RegisterPlan CreateSut()
        => new(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _userContext.Object,
            _employeeRepo.Object,
            _productRepo.Object,
            _dateService.Object,
            _calcService.Object,
            _deliveryService.Object,
            _billingService.Object,
            _factoryService.Object,
            _customerRepo.Object,
            _audit.Object);

    [Fact]
    public async Task Should_Fail_When_Validation_Fails()
    {
        var sut = CreateSut();

        var input = new RegisterPlanInput
        {
            CustomerId = Guid.NewGuid(),
            Items = new()
        };

        var result = await sut.Execute(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Not_Found()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        _employeeRepo.Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((EmployeeEntity?)null);

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Customer_Not_Found()
    {
        _setupEmployee();

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CustomerEntity?)null);

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Customer_Has_Open_Penalty()
    {
        _setupEmployee();
        _setupCustomer();

        _penaltyRepo.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContractPenalty>
            {
            CreatePenalty(ContractPenaltyStatus.PendingPayment)
            });

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Product_Not_Found()
    {
        _setupEmployee();
        _setupCustomer();

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProductEntity?)null);

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Tries_To_Apply_Discount()
    {
        _setupEmployee();
        _setupCustomer();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Employee));

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(10));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Calculation_Fails()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();
        _setupPlanGeneration();

        _calcService
            .Setup(x => x.CalculateContractTotal(It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<Discount?>()))
            .Returns(Result<Price>.Fail(Error.Validation("fail")));

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Create_Plan_Successfully()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();
        _setupPlanGeneration();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsSuccess.Should().BeTrue();

        _planRepo.Verify(x => x.AddAsync(It.IsAny<PlanEntity>()), Times.Once);
        _planRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Customer_Has_Debts_And_IgnoreWarnings_Is_False()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Billing>
            {
            new Billing(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(-5), Price.Create(100).Value!)
            });

        var sut = CreateSut();

        var input = CreateValidInput(null, false);

        var result = await sut.Execute(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_When_Customer_Has_Debts_But_IgnoreWarnings_Is_True()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();
        _setupPlanGeneration();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        _billingRepo.Setup(x => x.GetLateByCustomerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Billing>
            {
            new Billing(Guid.NewGuid(), 1, DateTime.UtcNow.AddDays(-5), Price.Create(100).Value!)
            });

        var sut = CreateSut();

        var input = CreateValidInput(null);

        var result = await sut.Execute(input);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_Role_Context_Fails()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Fail(Error.Forbidden("no role")));

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Invalid()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Fail(Error.Unauthorized("invalid user")));

        var sut = CreateSut();

        var result = await sut.Execute(CreateValidInput(null));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_Discount_When_User_Is_Manager()
    {
        _setupEmployee();
        _setupCustomer();
        _setupProducts();
        _setupPlanGeneration();

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        var sut = CreateSut();

        var input = CreateValidInput(10);

        var result = await sut.Execute(input);

        result.IsSuccess.Should().BeTrue();
    }

    private void _setupEmployee()
    {
        _userContext.Setup(x => x.GetUserId())
            .Returns(Result<Guid>.Success(_userId));

        _userContext.Setup(x => x.GetUserName())
            .Returns(Result<string>.Success("Employee"));

        _userContext.Setup(x => x.GetRole())
            .Returns(Result<Role>.Success(Role.Manager));

        var employee = new EmployeeEntity(
            EmployeeName.Create("Employee").Value!,
            Cpf.Create("05244777017").Value!,
            Email.Create("employee@email.com").Value!,
            Phone.Create("44999999999").Value!);

        employee.AssignUserId(_userId);

        _employeeRepo.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(employee);
    }

    private void _setupCustomer()
    {
        var customer = new CustomerEntity(
            CustomerName.Create("Customer").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("customer@email.com").Value!,
            Phone.Create("44999999998").Value!);

        _customerRepo.Setup(x => x.GetByIdAsync(_customerId))
            .ReturnsAsync(customer);
    }

    private void _setupProducts()
    {
        var product = new ProductEntity(
            ProductName.Create("Product").Value!,
            TypeProduct.Water,
            Price.Create(10).Value!,
            StockQuantity.Create(10).Value!);

        _productRepo.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
    }

    private void _setupPlanGeneration()
    {
        _dateService.Setup(x => x.GenerateStartDate(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(DateTime.UtcNow);

        _dateService.Setup(x => x.GenerateEndDate(
            It.IsAny<DateTime>(),
            It.IsAny<PlanCycle>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int?>()))
            .Returns(DateTime.UtcNow.AddMonths(1));

        _billingService.Setup(x => x.GenerateBillings(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<PlanCycle>(),
            It.IsAny<int>(),
            It.IsAny<Price>(),
            It.IsAny<int?>()))
            .Returns((Guid planId,
                DateTime startDate,
                DateTime endDate,
                PlanCycle cycle,
                int billingDay,
                Price amount,
                int? customMonths) =>
            [
                new Billing(planId, 1, startDate.AddDays(5), Price.Create(amount.Value).Value!),
                new Billing(planId, 2, startDate.AddDays(35), Price.Create(amount.Value).Value!)
            ]);

        _deliveryService.Setup(x => x.GenerateDeliveries(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<PlanCycle>(),
            It.IsAny<int>(),
            It.IsAny<int?>()))
            .Returns((Guid planId,
                DateTime startDate,
                DateTime endDate,
                PlanCycle cycle,
                int deliveryDay,
                int? customMonths) =>
            [
                new Delivery(planId, 1, startDate.AddDays(3))
            ]);

        _calcService.Setup(x => x.CalculateContractTotal(
            It.IsAny<decimal>(),
            It.IsAny<int>(),
            It.IsAny<Discount?>()))
            .Returns(Result<Price>.Success(Price.Create(240).Value!));

        _calcService.Setup(x => x.CalculateMonthlyBilling(
            It.IsAny<Price>(),
            It.IsAny<int>()))
            .Returns(Result<Price>.Success(Price.Create(120).Value!));

        _factoryService.Setup(x => x.CreatePlan(
            It.IsAny<RegisterPlanInput>(),
            It.IsAny<Guid>(),
            It.IsAny<PlanCycle>(),
            It.IsAny<Price>(),
            It.IsAny<Discount?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<int>()))
            .Returns((RegisterPlanInput input,
                Guid employeeId,
                PlanCycle cycle,
                Price total,
                Discount? discount,
                DateTime startDate,
                DateTime endDate,
                int deliveryDay,
                int billingDay) =>
                new PlanEntity(
                    input.CustomerId,
                    employeeId,
                    cycle,
                    total,
                    startDate,
                    endDate,
                    deliveryDay,
                    billingDay,
                    discount));

        _factoryService.Setup(x => x.CreateItems(
            It.IsAny<Guid>(),
            It.IsAny<List<RegisterPlanItemValidated>>()))
            .Returns((Guid planId, List<RegisterPlanItemValidated> items) =>
                items.Select(x => new PlanItem(planId, x.ProductId, x.Quantity)).ToList());
    }

    private static ContractPenalty CreatePenalty(ContractPenaltyStatus status)
    {
        var penalty = new ContractPenalty(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(100).Value!,
            Price.Create(100).Value!,
            Price.Create(100).Value!);

        if (status == ContractPenaltyStatus.PendingPayment)
            return penalty;

        if (status == ContractPenaltyStatus.Paid)
            penalty.Pay(Guid.NewGuid());

        if (status == ContractPenaltyStatus.Canceled)
            penalty.Cancel(Guid.NewGuid(), "reason with enough length");

        if (status == ContractPenaltyStatus.Waived)
            penalty.Waive(Guid.NewGuid(), "reason with enough length");

        return penalty;
    }

    private RegisterPlanInput CreateValidInput(int? discount, bool ignoreWarnings = true)
    {
        return new RegisterPlanInput
        {
            CustomerId = _customerId,
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 5,
            Discount = discount,
            IgnoreWarnings = ignoreWarnings,
            DurationInMonths = 12,
            Items = new List<PlanItemsInput>
        {
            new PlanItemsInput
            {
                ProductId = _productId,
                Quantity = 2
            }
        }
        };
    }
}

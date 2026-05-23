using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Plan.Application.UseCases;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Enums;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using ProductEntity = AquaGas.Product.Domain.Models.Product;

namespace AquaGas.Plan.Application.Tests.UseCases;

public class GetAllPlansTests
{
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IBillingRepository> _billingRepo = new();
    private readonly Mock<IContractPenaltyRepository> _penaltyRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetAllPlans CreateSut()
        => new(
            _planRepo.Object,
            _deliveryRepo.Object,
            _billingRepo.Object,
            _penaltyRepo.Object,
            _customerRepo.Object,
            _employeeRepo.Object,
            _productRepo.Object);

    private static PlanEntity CreatePlan()
    {
        var plan = new PlanEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(100).Value!,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            10,
            10,
            null);

        plan.AddItem(
            new PlanItem(
                plan.Id,
                Guid.NewGuid(),
                StockQuantity.Create(2).Value!));

        return plan;
    }

    private static CustomerEntity CreateCustomer()
        => new(
            CustomerName.Create("John Doe").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("john@doe.com").Value!,
            Phone.Create("44999999999").Value!);

    private static EmployeeEntity CreateEmployee()
        => new(
            EmployeeName.Create("Employee").Value!,
            Cpf.Create("05244777017").Value!,
            Email.Create("employee@company.com").Value!,
            Phone.Create("44999999998").Value!);

    private static ProductEntity CreateProduct()
        => new(
            ProductName.Create("Gas Cylinder").Value!,
            TypeProduct.Gas,
            Price.Create(50).Value!,
            StockQuantity.Create(10).Value!);

    private static Billing CreateBilling(
        Guid planId,
        BillingStatus status,
        DateTime? dueDate = null)
    {
        var billing = new Billing(
            planId,
            1,
            dueDate ?? DateTime.UtcNow.AddDays(1),
            Price.Create(100).Value!);

        switch (status)
        {
            case BillingStatus.Paid:
                billing.Pay(Guid.NewGuid());
                break;
            case BillingStatus.Late:
                billing = new Billing(
                    planId,
                    1,
                    DateTime.UtcNow.AddDays(-2),
                    Price.Create(100).Value!);
                billing.MarkAsLate();
                break;
        }

        return billing;
    }

    private static Delivery CreateDelivery(
        Guid planId,
        DeliveryStatus status,
        DateTime? dueDate = null)
    {
        var delivery = new Delivery(
            planId,
            1,
            dueDate ?? DateTime.UtcNow.AddDays(1));

        switch (status)
        {
            case DeliveryStatus.Delivered:
                delivery.Complete();
                break;
            case DeliveryStatus.Cancelled:
                delivery.Cancel();
                break;
            case DeliveryStatus.Late:
                delivery = new Delivery(
                    planId,
                    1,
                    DateTime.UtcNow.AddDays(-2));
                delivery.MarkAsLate();
                break;
        }

        return delivery;
    }

    private static ContractPenalty CreatePenalty(
        Guid planId,
        ContractPenaltyStatus status)
    {
        var penalty = new ContractPenalty(
            planId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContractPenaltyType.EarlyCancellation,
            Price.Create(10).Value!,
            Price.Create(10).Value!,
            Price.Create(10).Value!);

        if (status == ContractPenaltyStatus.Overdue)
        {
            typeof(ContractPenalty)
                .GetProperty(nameof(ContractPenalty.DueDate))!
                .SetValue(penalty, DateTime.UtcNow.Date.AddDays(-1));
        }

        if (status == ContractPenaltyStatus.Paid)
            penalty.Pay(Guid.NewGuid());

        return penalty;
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Plans()
    {
        _planRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<PlanEntity>());

        var sut = CreateSut();

        var result = await sut.Execute();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return_Plans_With_Full_Data()
    {
        var plan = CreatePlan();
        var customer = CreateCustomer();
        var employee = CreateEmployee();
        var product = CreateProduct();

        _planRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync([plan]);

        _customerRepo.Setup(x => x.GetByIdAsync(plan.CustomerId, false))
            .ReturnsAsync(customer);

        _employeeRepo.Setup(x => x.GetByIdAsync(plan.EmployeeId, false))
            .ReturnsAsync(employee);

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(product);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateDelivery(plan.Id, DeliveryStatus.Pending)
            ]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateBilling(plan.Id, BillingStatus.Pending)
            ]);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<ContractPenalty>());

        var sut = CreateSut();

        var result = await sut.Execute();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var output = result.Value!.First();

        output.Id.Should().Be(plan.Id);
        output.CustomerName.Should().Be(customer.Name.Value);
        output.EmployeeName.Should().Be(employee.Name.Value);
        output.Items.Should().HaveCount(1);
        output.Deliveries.Should().HaveCount(1);
        output.Billings.Should().HaveCount(1);
        output.Penalties.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Update_Deliveries_Billings_And_Penalties_When_Changed()
    {
        var plan = CreatePlan();
        var customer = CreateCustomer();
        var employee = CreateEmployee();
        var product = CreateProduct();

        _planRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync([plan]);

        _customerRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(customer);

        _employeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), false))
            .ReturnsAsync(employee);

        _productRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(product);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateDelivery(plan.Id, DeliveryStatus.Pending, DateTime.UtcNow.AddDays(-2))
            ]);

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreateBilling(plan.Id, BillingStatus.Pending, DateTime.UtcNow.AddDays(-2))
            ]);

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync([
                CreatePenalty(plan.Id, ContractPenaltyStatus.Overdue)
            ]);

        var sut = CreateSut();

        var result = await sut.Execute();

        result.IsSuccess.Should().BeTrue();

        _planRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _deliveryRepo.Verify(x => x.Update(It.IsAny<Delivery>()), Times.AtLeastOnce);
        _billingRepo.Verify(x => x.Update(It.IsAny<Billing>()), Times.AtLeastOnce);
        _penaltyRepo.Verify(x => x.Update(It.IsAny<ContractPenalty>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Handle_Missing_Customer_And_Employee()
    {
        var plan = CreatePlan();

        _planRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync([plan]);

        _customerRepo.Setup(x => x.GetByIdAsync(plan.CustomerId, false))
            .ReturnsAsync((CustomerEntity?)null);

        _employeeRepo.Setup(x => x.GetByIdAsync(plan.EmployeeId, false))
            .ReturnsAsync((EmployeeEntity?)null);

        _deliveryRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Delivery>());

        _billingRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<Billing>());

        _penaltyRepo.Setup(x => x.GetByPlanIdAsync(plan.Id))
            .ReturnsAsync(new List<ContractPenalty>());

        var sut = CreateSut();

        var result = await sut.Execute();

        result.IsSuccess.Should().BeTrue();

        var output = result.Value!.First();

        output.CustomerName.Should().BeEmpty();
        output.EmployeeName.Should().BeEmpty();
        output.CustomerId.Should().Be(Guid.Empty);
        output.EmployeeId.Should().Be(Guid.Empty);
    }
}

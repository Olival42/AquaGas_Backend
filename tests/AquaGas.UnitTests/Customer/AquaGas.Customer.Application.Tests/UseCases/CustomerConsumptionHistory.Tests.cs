using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.UseCases;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Product.Domain.Enums;
using ProductEntity = AquaGas.Product.Domain.Models.Product;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Product.Domain.ValueObjects;
using AquaGas.Sale.Domain.Models;
using SaleEntity = AquaGas.Sale.Domain.Models.Sale;
using AquaGas.Sale.Domain.Repositories;
using AquaGas.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Customer.Application.Tests.UseCases;

public sealed class CustomerConsumptionHistoryTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<IPlanRepository> _planRepository = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();

    private readonly CustomerConsumptionHistory _useCase;

    public CustomerConsumptionHistoryTests()
    {
        _useCase = new CustomerConsumptionHistory(
            _customerRepository.Object,
            _saleRepository.Object,
            _planRepository.Object,
            _deliveryRepository.Object,
            _productRepository.Object);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Customer_Does_Not_Exist()
    {
        var input = CreateInput();
        var id = Guid.NewGuid();

        _customerRepository.Setup(x => x.GetByIdAsync(id, true))
            .ReturnsAsync((CustomerEntity?)null);

        var result = await _useCase.Execute(id, input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message == "Customer not found");
    }

    [Fact]
    public async Task Should_Merge_Sales_And_Deliveries_Ordered_By_Date_Desc()
    {
        var customer = CreateCustomer();
        var input = CreateInput();
        var sale = CreateSale(customer.Id, new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc), false);
        var plan = CreatePlan(customer.Id, 120m);
        var delivery = CreateDeliveredDelivery(plan.Id, new DateTime(2026, 05, 12, 0, 0, 0, DateTimeKind.Utc));

        plan.AddItem(CreatePlanItem(plan.Id, sale.Items.First().ProductId, 2));

        MockBaseData(customer, sale, plan, delivery);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Type.Should().Be("Delivery");
        result.Value.Items[1].Type.Should().Be("Sale");
    }

    [Fact]
    public async Task Should_Compute_Summary_Only_With_Concluded_Operations()
    {
        var customer = CreateCustomer();
        var input = CreateInput();
        var sale = CreateSale(customer.Id, new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc), false, total: 50m, quantity: 2);
        var plan = CreatePlan(customer.Id, 120m);
        var delivered = CreateDeliveredDelivery(plan.Id, new DateTime(2026, 05, 12, 0, 0, 0, DateTimeKind.Utc));
        var pending = CreatePendingDelivery(plan.Id, new DateTime(2026, 05, 14, 0, 0, 0, DateTimeKind.Utc));

        plan.AddItem(CreatePlanItem(plan.Id, sale.Items.First().ProductId, 3));

        MockBaseData(customer, sale, plan, [delivered, pending]);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Summary.TotalSales.Should().Be(1);
        result.Value.Summary.TotalDeliveries.Should().Be(1);
        result.Value.Summary.TotalSpent.Should().Be(110m);
        result.Value.Summary.TotalItems.Should().Be(5);
        result.Value.Summary.AverageTicket.Should().Be(55m);
    }

    [Fact]
    public async Task Should_Not_Return_Canceled_Sales_Or_NonDelivered_Deliveries()
    {
        var customer = CreateCustomer();
        var input = CreateInput();
        var finishedSale = CreateSale(customer.Id, new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc), false);
        var canceledSale = CreateSale(customer.Id, new DateTime(2026, 05, 11, 0, 0, 0, DateTimeKind.Utc), true);
        var plan = CreatePlan(customer.Id, 120m);
        var delivered = CreateDeliveredDelivery(plan.Id, new DateTime(2026, 05, 12, 0, 0, 0, DateTimeKind.Utc));
        var pending = CreatePendingDelivery(plan.Id, new DateTime(2026, 05, 14, 0, 0, 0, DateTimeKind.Utc));
        var late = CreateLateDelivery(plan.Id, new DateTime(2026, 05, 15, 0, 0, 0, DateTimeKind.Utc));
        var canceled = CreateCanceledDelivery(plan.Id, new DateTime(2026, 05, 16, 0, 0, 0, DateTimeKind.Utc));

        plan.AddItem(CreatePlanItem(plan.Id, finishedSale.Items.First().ProductId, 2));

        MockBaseData(customer, [finishedSale, canceledSale], [plan], [delivered, pending, late, canceled]);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Return_Full_Consolidated_History_Without_Pagination()
    {
        var customer = CreateCustomer();
        var input = CreateInput();
        var sale = CreateSale(customer.Id, new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc), false);
        var plan = CreatePlan(customer.Id, 120m);
        var delivery = CreateDeliveredDelivery(plan.Id, new DateTime(2026, 05, 12, 0, 0, 0, DateTimeKind.Utc));

        plan.AddItem(CreatePlanItem(plan.Id, sale.Items.First().ProductId, 2));

        MockBaseData(customer, sale, plan, delivery);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Type.Should().Be("Delivery");
        result.Value.Items[1].Type.Should().Be("Sale");
    }

    [Fact]
    public async Task Should_Return_Empty_History_When_Customer_Has_No_Consumption()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        _customerRepository.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _saleRepository.Setup(x => x.GetByCustomerIdWithItemsAsync(customer.Id))
            .ReturnsAsync([]);

        _planRepository.Setup(x => x.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();

        result.Value.Summary.TotalSales.Should().Be(0);
        result.Value.Summary.TotalDeliveries.Should().Be(0);
        result.Value.Summary.TotalSpent.Should().Be(0);
        result.Value.Summary.TotalItems.Should().Be(0);
        result.Value.Summary.AverageTicket.Should().Be(0);
    }

    [Fact]
    public async Task Should_Filter_Items_Outside_Period()
    {
        var customer = CreateCustomer();

        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 05, 20, 23, 59, 59, DateTimeKind.Utc)
        };

        var validSale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 15, 0, 0, 0, DateTimeKind.Utc),
            false);

        var outsideSale = CreateSale(
            customer.Id,
            new DateTime(2026, 04, 10, 0, 0, 0, DateTimeKind.Utc),
            false);

        var plan = CreatePlan(customer.Id, 120m);

        plan.AddItem(CreatePlanItem(
            plan.Id,
            validSale.Items.First().ProductId,
            2));

        var validDelivery = new Delivery(
            plan.Id,
            1,
            new DateTime(2026, 05, 18, 0, 0, 0, DateTimeKind.Utc));

        typeof(Delivery)
            .GetProperty(nameof(Delivery.Status))!
            .SetValue(validDelivery, DeliveryStatus.Delivered);

        typeof(Delivery)
            .GetProperty(nameof(Delivery.DeliveryDate))!
            .SetValue(validDelivery,
                new DateTime(2026, 05, 18, 0, 0, 0, DateTimeKind.Utc));

        var outsideDelivery = new Delivery(
            plan.Id,
            2,
            new DateTime(2026, 04, 18, 0, 0, 0, DateTimeKind.Utc));

        typeof(Delivery)
            .GetProperty(nameof(Delivery.Status))!
            .SetValue(outsideDelivery, DeliveryStatus.Delivered);

        typeof(Delivery)
            .GetProperty(nameof(Delivery.DeliveryDate))!
            .SetValue(outsideDelivery,
                new DateTime(2026, 04, 18, 0, 0, 0, DateTimeKind.Utc));

        MockBaseData(
            customer,
            [validSale, outsideSale],
            [plan],
            [validDelivery, outsideDelivery]);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Return_Unknown_When_Product_Does_Not_Exist()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        var sale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            false);

        _customerRepository.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _saleRepository.Setup(x => x.GetByCustomerIdWithItemsAsync(customer.Id))
            .ReturnsAsync([sale]);

        _planRepository.Setup(x => x.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync([]);

        _deliveryRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);

        var productId = sale.Items.First().ProductId;

        _productRepository.Setup(x => x.GetByIdAsync(productId, false))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Items.First()
            .Products.First()
            .Name.Should().Be("Unknown");
    }

    [Fact]
    public async Task Should_Allocate_Delivery_Value_Equally_Between_Deliveries()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        var sale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            false);

        var plan = CreatePlan(customer.Id, 300m);

        plan.AddItem(CreatePlanItem(plan.Id, sale.Items.First().ProductId, 2));

        var delivery1 = CreateDeliveredDelivery(
            plan.Id,
            new DateTime(2026, 05, 12, 0, 0, 0, DateTimeKind.Utc));

        var delivery2 = CreateDeliveredDelivery(
            plan.Id,
            new DateTime(2026, 05, 15, 0, 0, 0, DateTimeKind.Utc));

        var delivery3 = CreateDeliveredDelivery(
            plan.Id,
            new DateTime(2026, 05, 18, 0, 0, 0, DateTimeKind.Utc));

        MockBaseData(
            customer,
            [sale],
            [plan],
            [delivery1, delivery2, delivery3]);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        var deliveryItems = result.Value!.Items
            .Where(x => x.Type == "Delivery")
            .ToList();

        deliveryItems.Should().HaveCount(3);

        deliveryItems.Should()
            .OnlyContain(x => x.Value == 100m);
    }

    [Fact]
    public async Task Should_Order_Items_By_Date_Descending()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        var olderSale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            false);

        var newerSale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 15, 0, 0, 0, DateTimeKind.Utc),
            false);

        MockBaseData(
            customer,
            [olderSale, newerSale],
            [],
            []);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Items.Should().HaveCount(2);

        result.Value.Items[0].Date
            .Should().BeAfter(result.Value.Items[1].Date);
    }

    [Fact]
    public async Task Should_Use_DeliveryDate_When_Available()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        var sale = CreateSale(
            customer.Id,
            new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            false);

        var plan = CreatePlan(customer.Id, 120m);

        plan.AddItem(CreatePlanItem(plan.Id, sale.Items.First().ProductId, 1));

        var delivery = new Delivery(
            plan.Id,
            1,
            new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc));

        delivery.Complete();
        SetDeliveryDate(delivery, new DateTime(2026, 05, 20, 0, 0, 0, DateTimeKind.Utc));

        MockBaseData(customer, sale, plan, delivery);

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        var deliveryItem = result.Value!.Items
            .First(x => x.Type == "Delivery");

        deliveryItem.Date.Should().Be(delivery.DeliveryDate);
    }

    [Fact]
    public async Task Should_Call_ProductRepository_Only_Once_Per_Product()
    {
        var customer = CreateCustomer();
        var input = CreateInput();

        var productId = Guid.NewGuid();

        var sale = new SaleEntity(
            customer.Id,
            Guid.NewGuid(),
            Price.Create(100m).Value!,
            null,
            DateTime.UtcNow);

        sale.AddItem(new SaleItem(
            sale.Id,
            productId,
            StockQuantity.Create(2).Value!,
            Price.Create(100m).Value!));

        var plan = CreatePlan(customer.Id, 120m);

        plan.AddItem(CreatePlanItem(plan.Id, productId, 3));

        var delivery = CreateDeliveredDelivery(plan.Id, DateTime.UtcNow);

        _customerRepository.Setup(x => x.GetByIdAsync(customer.Id))
            .ReturnsAsync(customer);

        _saleRepository.Setup(x => x.GetByCustomerIdWithItemsAsync(customer.Id))
            .ReturnsAsync([sale]);

        _planRepository.Setup(x => x.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync([plan]);

        _deliveryRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([delivery]);

        _productRepository.Setup(x => x.GetByIdAsync(productId, false))
            .ReturnsAsync(CreateProduct(productId));

        var result = await _useCase.Execute(customer.Id, input);

        result.IsSuccess.Should().BeTrue();

        _productRepository.Verify(
            x => x.GetByIdAsync(productId, false),
            Times.Once);
    }

    private void MockBaseData(
        CustomerEntity customer,
        SaleEntity sale,
        PlanEntity plan,
        Delivery delivery)
        => MockBaseData(customer, [sale], [plan], [delivery]);

    private void MockBaseData(
        CustomerEntity customer,
        SaleEntity sale,
        PlanEntity plan,
        IEnumerable<Delivery> deliveries)
        => MockBaseData(customer, [sale], [plan], deliveries);

    private void MockBaseData(
        CustomerEntity customer,
        IEnumerable<SaleEntity> sales,
        IEnumerable<PlanEntity> plans,
        IEnumerable<Delivery> deliveries)
    {
        _customerRepository.Setup(x => x.GetByIdAsync(customer.Id, true))
            .ReturnsAsync(customer);

        _saleRepository.Setup(x => x.GetByCustomerIdWithItemsAsync(customer.Id))
            .ReturnsAsync(sales);

        var planList = plans.ToList();

        _planRepository.Setup(x => x.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync(planList);

        _deliveryRepository.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(deliveries.ToList());

        var productIds = sales
            .SelectMany(x => x.Items.Select(i => i.ProductId))
            .Concat(planList.SelectMany(x => x.Items.Select(i => i.ProductId)))
            .Distinct()
            .ToList();

        foreach (var productId in productIds)
        {
            _productRepository.Setup(x => x.GetByIdAsync(productId, false))
                .ReturnsAsync(CreateProduct(productId));
        }
    }

    private static CustomerConsumptionHistoryInput CreateInput()
    {
        return new CustomerConsumptionHistoryInput
        {
            StartDate = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 05, 31, 23, 59, 59, DateTimeKind.Utc),
        };
    }

    private static CustomerEntity CreateCustomer()
    {
        return new CustomerEntity(
            CustomerName.Create("Cliente Teste").Value!,
            Document.Create("55964416004").Value!,
            Email.Create("cliente@aquagas.com").Value!,
            Phone.Create("44999999999").Value!);
    }

    private static SaleEntity CreateSale(
        Guid customerId,
        DateTime date,
        bool canceled,
        decimal total = 50m,
        int quantity = 2)
    {
        var sale = new SaleEntity(
            customerId,
            Guid.NewGuid(),
            Price.Create(total).Value!,
            null,
            date);

        sale.AddItem(new SaleItem(
            sale.Id,
            Guid.NewGuid(),
            StockQuantity.Create(quantity).Value!,
            Price.Create(total).Value!));

        if (canceled)
            sale.Cancel("Cliente desistiu");

        return sale;
    }

    private static PlanEntity CreatePlan(Guid customerId, decimal total)
    {
        return new PlanEntity(
            customerId,
            Guid.NewGuid(),
            PlanCycle.Monthly,
            Price.Create(total).Value!,
            new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
            10,
            5);
    }

    private static PlanItem CreatePlanItem(Guid planId, Guid productId, int quantity)
    {
        return new PlanItem(
            planId,
            productId,
            StockQuantity.Create(quantity).Value!);
    }

    private static Delivery CreateDeliveredDelivery(Guid planId, DateTime deliveryDate)
    {
        var delivery = new Delivery(planId, 1, deliveryDate.AddDays(-2));
        delivery.Complete();
        SetDeliveryDate(delivery, deliveryDate);
        return delivery;
    }

    private static void SetDeliveryDate(Delivery delivery, DateTime deliveryDate)
    {
        typeof(Delivery)
            .GetProperty(nameof(Delivery.DeliveryDate))!
            .SetValue(delivery, deliveryDate);
    }

    private static Delivery CreatePendingDelivery(Guid planId, DateTime dueDate)
        => new(planId, 2, dueDate);

    private static Delivery CreateLateDelivery(Guid planId, DateTime dueDate)
    {
        var delivery = new Delivery(planId, 3, dueDate.AddDays(-10));
        delivery.MarkAsLate();
        return delivery;
    }

    private static Delivery CreateCanceledDelivery(Guid planId, DateTime dueDate)
    {
        var delivery = new Delivery(planId, 4, dueDate);
        delivery.Cancel();
        return delivery;
    }

    private static ProductEntity CreateProduct(Guid productId)
    {
        var product = new ProductEntity(
            ProductName.Create("Botijao 13kg").Value!,
            TypeProduct.Gas,
            Price.Create(100m).Value!,
            StockQuantity.Create(100).Value!);

        typeof(ProductEntity).GetProperty(nameof(ProductEntity.Id))!
            .SetValue(product, productId);

        return product;
    }
}

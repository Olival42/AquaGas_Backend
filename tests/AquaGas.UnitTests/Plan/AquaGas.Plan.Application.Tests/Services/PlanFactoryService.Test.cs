using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class PlanFactoryServiceTests
{
    private readonly PlanFactoryService _service = new();

    [Fact]
    public void Should_Create_Plan_Successfully()
    {
        var input = new RegisterPlanInput
        {
            CustomerId = Guid.NewGuid(),
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 15,
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            ]
        };

        var employeeId = Guid.NewGuid();

        var total = Price.Create(150)
            .Value!;

        var discount = Discount.Create(10)
            .Value!;

        var startDate = DateTime.UtcNow.Date;

        var endDate = startDate.AddMonths(1);

        var result = _service.CreatePlan(
            input,
            employeeId,
            PlanCycle.Monthly,
            total,
            discount,
            startDate,
            endDate,
            input.DeliveryDay,
            input.BillingDay);

        result.Should().NotBeNull();

        result.CustomerId.Should().Be(input.CustomerId);

        result.EmployeeId.Should().Be(employeeId);

        result.Cycle.Should().Be(PlanCycle.Monthly);

        result.Total.Should().Be(total);

        result.CurrentDiscount.Should().Be(discount);

        result.StartDate.Should().Be(startDate);

        result.EndDate.Should().Be(endDate);

        result.DeliveryDay.Should().Be(input.DeliveryDay);

        result.BillingDay.Should().Be(input.BillingDay);
    }

    [Fact]
    public void Should_Create_Plan_Without_Discount()
    {
        var input = new RegisterPlanInput
        {
            CustomerId = Guid.NewGuid(),
            Cycle = "Monthly",
            DeliveryDay = 5,
            BillingDay = 20,
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        var total = Price.Create(100)
            .Value!;

        var result = _service.CreatePlan(
            input,
            Guid.NewGuid(),
            PlanCycle.Monthly,
            total,
            null,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddMonths(1),
            input.DeliveryDay,
            input.BillingDay);

        result.Should().NotBeNull();

        result.CurrentDiscount.Should().BeNull();
    }

    [Fact]
    public void Should_Create_Items_Successfully()
    {
        var planId = Guid.NewGuid();

        var quantity1 = StockQuantity.Create(2)
            .Value!;

        var quantity2 = StockQuantity.Create(5)
            .Value!;

        var items = new List<RegisterPlanItemValidated>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                Quantity = quantity1
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                Quantity = quantity2
            }
        };

        var result = _service.CreateItems(
            planId,
            items);

        result.Should().NotBeNull();

        result.Should().HaveCount(2);

        result[0].PlanId.Should().Be(planId);

        result[0].ProductId.Should().Be(items[0].ProductId);

        result[0].Quantity.Should().Be(quantity1);

        result[1].PlanId.Should().Be(planId);

        result[1].ProductId.Should().Be(items[1].ProductId);

        result[1].Quantity.Should().Be(quantity2);
    }

    [Fact]
    public void Should_Return_Empty_List_When_Items_Are_Empty()
    {
        var result = _service.CreateItems(
            Guid.NewGuid(),
            []);

        result.Should().NotBeNull();

        result.Should().BeEmpty();
    }
}
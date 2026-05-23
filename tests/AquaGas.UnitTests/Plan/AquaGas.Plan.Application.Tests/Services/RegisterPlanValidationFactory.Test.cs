using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Services;
using AquaGas.Plan.Domain.Enums;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Services;

public sealed class RegisterPlanValidationFactoryTests
{
    private static RegisterPlanInput CreateValidInput()
    {
        return new RegisterPlanInput
        {
            CustomerId = Guid.NewGuid(),
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 15,
            DurationInMonths = null,
            Discount = null,
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            ]
        };
    }

    [Fact]
    public void Should_Return_Success_When_Input_Is_Valid()
    {
        var input = CreateValidInput();

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.CustomerId.Should().Be(input.CustomerId);

        result.Value.Cycle.Should().Be(PlanCycle.Monthly);

        result.Value.Items.Should().HaveCount(1);

        result.Value.Items[0].ProductId
            .Should()
            .Be(input.Items[0].ProductId);
    }

    [Fact]
    public void Should_Return_Failure_When_Cycle_Is_Invalid()
    {
        var input = CreateValidInput() with
        {
            Cycle = "InvalidCycle"
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Return_Failure_When_Item_Quantity_Is_Invalid()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -1
                }
            ]
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Return_Failure_When_Multiple_Items_Have_Invalid_Quantities()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -1
                },
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -2
                }
            ]
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Create_All_Validated_Items_When_Items_Are_Valid()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                },
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 3
                }
            ]
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Return_Success_When_Discount_Is_Null()
    {
        var input = CreateValidInput() with
        {
            Discount = null
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Discount.Should().BeNull();
    }

    [Fact]
    public void Should_Return_Failure_When_Discount_Is_Invalid()
    {
        var input = CreateValidInput() with
        {
            Discount = -10
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsFailure.Should().BeTrue();

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Preserve_DurationInMonths_When_Provided()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Custom",
            DurationInMonths = 12
        };

        var result = RegisterPlanValidationFactory.Combine(input);

        result.IsSuccess.Should().BeTrue();

        result.Value!.DurationInMonths.Should().Be(12);
    }
}

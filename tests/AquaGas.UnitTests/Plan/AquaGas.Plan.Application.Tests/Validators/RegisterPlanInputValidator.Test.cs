using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class RegisterPlanInputValidatorTests
{
    private readonly RegisterPlanInputValidator _validator = new();

    private static RegisterPlanInput CreateValidInput()
    {
        return new RegisterPlanInput
        {
            CustomerId = Guid.NewGuid(),
            Cycle = "Monthly",
            DeliveryDay = 10,
            BillingDay = 15,
            DurationInMonths = null,
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
    public void Should_Pass_When_Input_Is_Valid()
    {
        var input = CreateValidInput();

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_CustomerId_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            CustomerId = Guid.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "CustomerId is required");
    }

    [Fact]
    public void Should_Fail_When_Cycle_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            Cycle = string.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "Cycle is required");
    }

    [Fact]
    public void Should_Fail_When_DeliveryDay_Is_Less_Than_1()
    {
        var input = CreateValidInput() with
        {
            DeliveryDay = 0
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DeliveryDay must be between 1 and 31");
    }

    [Fact]
    public void Should_Fail_When_DeliveryDay_Is_Greater_Than_31()
    {
        var input = CreateValidInput() with
        {
            DeliveryDay = 32
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DeliveryDay must be between 1 and 31");
    }

    [Fact]
    public void Should_Fail_When_BillingDay_Is_Less_Than_1()
    {
        var input = CreateValidInput() with
        {
            BillingDay = 0
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "BillingDay must be between 1 and 31");
    }

    [Fact]
    public void Should_Fail_When_BillingDay_Is_Greater_Than_31()
    {
        var input = CreateValidInput() with
        {
            BillingDay = 40
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "BillingDay must be between 1 and 31");
    }

    [Fact]
    public void Should_Fail_When_Items_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            Items = []
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Plan must contain at least one item");
    }

    [Fact]
    public void Should_Fail_When_Items_Have_Duplicate_Products()
    {
        var productId = Guid.NewGuid();

        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = productId,
                    Quantity = 1
                },
                new PlanItemsInput
                {
                    ProductId = productId,
                    Quantity = 2
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duplicate products are not allowed in the plan items");
    }

    [Fact]
    public void Should_Fail_When_Item_ProductId_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.Empty,
                    Quantity = 2
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "ProductId is required");
    }

    [Fact]
    public void Should_Fail_When_Item_Quantity_Is_Invalid()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new PlanItemsInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Quantity must be greater than zero");
    }

    [Fact]
    public void Should_Pass_When_Custom_Cycle_Has_Valid_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Custom",
            DurationInMonths = 12
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Has_Null_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Custom",
            DurationInMonths = null
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DurationInMonths is required for custom plans");
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Duration_Is_Less_Than_2()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Custom",
            DurationInMonths = 1
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DurationInMonths for custom plans must be between 2 and 60 months");
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Duration_Is_Greater_Than_60()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Custom",
            DurationInMonths = 61
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DurationInMonths for custom plans must be between 2 and 60 months");
    }

    [Fact]
    public void Should_Fail_When_Non_Custom_Cycle_Has_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Monthly",
            DurationInMonths = 12
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "DurationInMonths should only be sent for custom plans");
    }

    [Fact]
    public void Should_Pass_When_Non_Custom_Cycle_Has_Null_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = "Annual",
            DurationInMonths = null
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }
}
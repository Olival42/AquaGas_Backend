using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;
using AquaGas.Plan.Domain.Enums;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class DowngradePlanInputValidatorTests
{
    private readonly DowngradePlanInputValidator _validator = new();

    private static DowngradePlanInput CreateValidInput()
    {
        return new DowngradePlanInput
        {
            Cycle = PlanCycle.Monthly,
            DurationInMonths = null,
            Reason = "Customer requested downgrade",
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
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
    public void Should_Fail_When_No_Downgrade_Changes_Are_Informed()
    {
        var input = CreateValidInput() with
        {
            Cycle = null,
            Items = null
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "At least one downgrade change must be informed: Cycle or Items.");
    }

    [Fact]
    public void Should_Fail_When_Cycle_Is_Invalid()
    {
        var input = CreateValidInput() with
        {
            Cycle = (PlanCycle)999
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "Invalid plan cycle.");
    }

    [Fact]
    public void Should_Pass_When_Custom_Cycle_Has_Valid_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = PlanCycle.Custom,
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
            Cycle = PlanCycle.Custom,
            DurationInMonths = null
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months is required for a custom cycle.");
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Has_Invalid_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = PlanCycle.Custom,
            DurationInMonths = 0
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months must be greater than zero.");
    }

    [Fact]
    public void Should_Fail_When_Non_Custom_Cycle_Has_Duration()
    {
        var input = CreateValidInput() with
        {
            Cycle = PlanCycle.Monthly,
            DurationInMonths = 10
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months must be null when cycle is not custom.");
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
                "Items list cannot be empty when provided.");
    }

    [Fact]
    public void Should_Fail_When_Items_Have_Duplicate_Products()
    {
        var productId = Guid.NewGuid();

        var input = CreateValidInput() with
        {
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = productId,
                    Quantity = 1
                },
                new DowngradePlanItemInput
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
                "Duplicate products are not allowed.");
    }

    [Fact]
    public void Should_Fail_When_Item_ProductId_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = Guid.Empty,
                    Quantity = 1
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "ProductId is required.");
    }

    [Fact]
    public void Should_Fail_When_Item_Quantity_Is_Negative()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -1
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Quantity must be zero or greater.");
    }

    [Fact]
    public void Should_Pass_When_Item_Quantity_Is_Zero()
    {
        var input = CreateValidInput() with
        {
            Items =
            [
                new DowngradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            Reason = string.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "Reason is required.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Too_Short()
    {
        var input = CreateValidInput() with
        {
            Reason = "abc"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason must be at least 5 characters long.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Too_Long()
    {
        var input = CreateValidInput() with
        {
            Reason = new string('a', 501)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason cannot exceed 500 characters.");
    }
}
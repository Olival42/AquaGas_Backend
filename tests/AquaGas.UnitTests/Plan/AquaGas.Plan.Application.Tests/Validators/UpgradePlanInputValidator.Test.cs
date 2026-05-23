using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;
using AquaGas.Plan.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class UpgradePlanInputValidatorTests
{
    private readonly UpgradePlanInputValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Cycle_Is_Provided()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Monthly
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_Items_Are_Provided()
    {
        var input = new UpgradePlanInput
        {
            Items =
            [
                new UpgradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_No_Changes_Are_Provided()
    {
        var input = new UpgradePlanInput();

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "At least one change must be informed: Cycle or Items.");
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Without_Duration()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Custom
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months is required for a custom cycle.");
    }

    [Fact]
    public void Should_Fail_When_Custom_Cycle_Has_Invalid_Duration()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Custom,
            DurationInMonths = 0
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months must be greater than zero.");
    }

    [Fact]
    public void Should_Pass_When_Custom_Cycle_With_Valid_Duration()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Custom,
            DurationInMonths = 6
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Non_Custom_Cycle_Has_Duration()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Monthly,
            DurationInMonths = 3
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duration in months must be null when cycle is not custom.");
    }

    [Fact]
    public void Should_Fail_When_Items_List_Is_Empty()
    {
        var input = new UpgradePlanInput
        {
            Items = []
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Items list cannot be empty when provided.");
    }

    [Fact]
    public void Should_Fail_When_Items_Have_Duplicate_ProductIds()
    {
        var productId = Guid.NewGuid();

        var input = new UpgradePlanInput
        {
            Items =
            [
                new UpgradePlanItemInput
                {
                    ProductId = productId,
                    Quantity = 1
                },
                new UpgradePlanItemInput
                {
                    ProductId = productId,
                    Quantity = 2
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Duplicate products are not allowed.");
    }

    [Fact]
    public void Should_Fail_When_ProductId_Is_Empty()
    {
        var input = new UpgradePlanInput
        {
            Items =
            [
                new UpgradePlanItemInput
                {
                    ProductId = Guid.Empty,
                    Quantity = 1
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "ProductId is required.");
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Zero()
    {
        var input = new UpgradePlanInput
        {
            Items =
            [
                new UpgradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Quantity must be greater than zero.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Exceeds_Max_Length()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Monthly,
            Reason = new string('a', 501)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason cannot exceed 500 characters.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Complete_Input()
    {
        var input = new UpgradePlanInput
        {
            Cycle = PlanCycle.Custom,
            DurationInMonths = 12,
            Reason = "Upgrade solicitado pelo cliente.",
            Items =
            [
                new UpgradePlanItemInput
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 5
                }
            ]
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }
}
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class RegisterPlanItemsInputValidatorTests
{
    private readonly RegisterPlanItemsInputValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Input_Is_Valid()
    {
        var input = new PlanItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProductId_Is_Empty()
    {
        var input = new PlanItemsInput
        {
            ProductId = Guid.Empty,
            Quantity = 5
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "ProductId is required");
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Zero()
    {
        var input = new PlanItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Quantity must be greater than zero");
    }

    [Fact]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        var input = new PlanItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = -10
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Quantity must be greater than zero");
    }

    [Fact]
    public void Should_Pass_When_Quantity_Is_One()
    {
        var input = new PlanItemsInput
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }
}
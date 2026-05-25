using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AquaGas.Customer.Application.Tests.Validators;

public sealed class CustomerConsumptionHistoryValidatorTests
{
    private readonly CustomerConsumptionHistoryValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Input_Is_Valid()
    {
        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 31)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_When_StartDate_Is_Empty()
    {
        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = default,
            EndDate = new DateTime(2026, 05, 31)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.PropertyName == "StartDate" &&
                x.ErrorMessage == "StartDate is required");
    }

    [Fact]
    public void Should_Fail_When_EndDate_Is_Empty()
    {
        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = new DateTime(2026, 05, 01),
            EndDate = default
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.PropertyName == "EndDate" &&
                x.ErrorMessage == "EndDate is required");
    }

    [Fact]
    public void Should_Fail_When_StartDate_Is_Greater_Than_EndDate()
    {
        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = new DateTime(2026, 06, 01),
            EndDate = new DateTime(2026, 05, 01)
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.PropertyName == "Period" &&
                x.ErrorMessage == "StartDate must be less than or equal to EndDate");
    }

    [Fact]
    public void Should_Pass_When_StartDate_Is_Equal_To_EndDate()
    {
        var sameDate = new DateTime(2026, 05, 10);

        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = sameDate,
            EndDate = sameDate
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Multiple_Errors_When_Input_Is_Completely_Invalid()
    {
        var input = new CustomerConsumptionHistoryInput
        {
            StartDate = default,
            EndDate = default
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);

        result.Errors.Should()
            .Contain(x => x.PropertyName == "StartDate");

        result.Errors.Should()
            .Contain(x => x.PropertyName == "EndDate");
    }
}
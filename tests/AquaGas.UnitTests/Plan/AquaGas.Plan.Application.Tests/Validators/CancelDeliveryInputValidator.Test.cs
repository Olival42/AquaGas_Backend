using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class CancelDeliveryInputValidatorTests
{
    private readonly CancelDeliveryInputValidator _validator = new();

    private static CancelDeliveryInput CreateValidInput()
    {
        return new CancelDeliveryInput
        {
            DeliveryId = Guid.NewGuid(),
            Reason = "Customer requested cancellation"
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
    public void Should_Fail_When_DeliveryId_Is_Empty()
    {
        var input = CreateValidInput() with
        {
            DeliveryId = Guid.Empty
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "DeliveryId is required");
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
                x.ErrorMessage == "Reason is required");
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
                "Reason must have at least 5 characters");
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
                "Reason must not exceed 500 characters");
    }
}
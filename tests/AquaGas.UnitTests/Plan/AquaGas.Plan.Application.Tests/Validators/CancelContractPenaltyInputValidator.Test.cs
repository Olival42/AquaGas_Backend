using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;

using FluentAssertions;

using Xunit;

namespace AquaGas.Plan.Application.Tests.Validators;

public sealed class CancelContractPenaltyInputValidatorTests
{
    private readonly CancelContractPenaltyInputValidator _validator = new();

    private static CancelContractPenaltyInput CreateValidInput()
    {
        return new CancelContractPenaltyInput
        {
            Reason = "Customer requested contract cancellation"
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
            Reason = "Too short"
        };

        var result = _validator.Validate(input);

        result.IsValid.Should().BeFalse();

        result.Errors.Should()
            .Contain(x =>
                x.ErrorMessage ==
                "Reason must be at least 10 characters long.");
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
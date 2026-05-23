using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class WaiveContractPenaltyInputValidatorTests
{
    private readonly WaiveContractPenaltyInputValidator _validator;

    public WaiveContractPenaltyInputValidatorTests()
    {
        _validator = new WaiveContractPenaltyInputValidator();
    }

    [Fact]
    public void Should_Pass_When_Reason_Is_Valid()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = "Cliente com problema recorrente no sistema"
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = ""
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason is required");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Null()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = null!
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason is required");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Less_Than_10_Characters()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = "Muito bom"
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason must contain at least 10 characters");
    }

    [Fact]
    public void Should_Fail_When_Reason_Exceeds_500_Characters()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = new string('A', 501)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Pass_When_Reason_Has_Exactly_10_Characters()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = "1234567890"
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Pass_When_Reason_Has_Exactly_500_Characters()
    {
        var input = new WaiveContractPenaltyInput
        {
            Reason = new string('A', 500)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
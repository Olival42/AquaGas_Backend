using AquaGas.Sale.Application.Dtos.Requests;
using AquaGas.Sale.Application.Validations;

using FluentValidation.TestHelper;

using Xunit;

public class CancelSaleValidatorTests
{
    private readonly CancelSaleValidator _validator =
        new();

    [Fact]
    public void Should_Not_Have_Error_When_Reason_Is_Valid()
    {
        var input = new CancelSaleInput
        {
            Reason = "Customer canceled the purchase"
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(
            x => x.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var input = new CancelSaleInput
        {
            Reason = string.Empty
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(
            x => x.Reason)
            .WithErrorMessage(
                "Reason is required");
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Null()
    {
        var input = new CancelSaleInput
        {
            Reason = null!
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(
            x => x.Reason)
            .WithErrorMessage(
                "Reason is required");
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Contains_Only_Spaces()
    {
        var input = new CancelSaleInput
        {
            Reason = "     "
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(
            x => x.Reason)
            .WithErrorMessage(
                "Reason cannot contain only spaces");
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Has_Less_Than_2_Characters()
    {
        var input = new CancelSaleInput
        {
            Reason = "A"
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(
            x => x.Reason)
            .WithErrorMessage(
                "Reason must contain at least 2 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Exceeds_500_Characters()
    {
        var input = new CancelSaleInput
        {
            Reason = new string('A', 501)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(
            x => x.Reason)
            .WithErrorMessage(
                "Reason must contain at most 500 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Reason_Has_Exactly_2_Characters()
    {
        var input = new CancelSaleInput
        {
            Reason = "Ok"
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(
            x => x.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Reason_Has_Exactly_500_Characters()
    {
        var input = new CancelSaleInput
        {
            Reason = new string('A', 500)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor(
            x => x.Reason);
    }
}
using AquaGas.Product.Domain.Enums;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Validators;
using FluentValidation.TestHelper;

namespace AquaGas.Report.Application.Tests.Validators;

public sealed class StockMovementReportInputValidatorTests
{
    private readonly StockMovementReportInputValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Start_Is_Greater_Than_End()
    {
        var input = new StockMovementReportInput
        {
            Start = new DateTime(2026, 05, 02, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Input_Is_Valid()
    {
        var input = new StockMovementReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Start_Is_Empty()
    {
        var input = new StockMovementReportInput
        {
            Start = default,
            End = new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Start")
            .WithErrorMessage("Start is required");
    }

    [Fact]
    public void Should_Have_Error_When_End_Is_Empty()
    {
        var input = new StockMovementReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = default
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("End")
            .WithErrorMessage("End is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Start_Is_Equal_To_End()
    {
        var date = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc);

        var input = new StockMovementReportInput
        {
            Start = date,
            End = date
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Return_Multiple_Errors_When_Input_Is_Completely_Invalid()
    {
        var input = new StockMovementReportInput
        {
            Start = default,
            End = default,
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Start");
        result.ShouldHaveValidationErrorFor("End");
    }
}

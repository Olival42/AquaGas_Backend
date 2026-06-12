using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Validators;
using FluentValidation.TestHelper;

namespace AquaGas.Report.Application.Tests.Validators;

public sealed class SalesReportInputValidatorTests
{
    private readonly SalesReportInputValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Start_Is_Empty()
    {
        var input = new SalesReportInput
        {
            Start = default,
            End = new DateTime(2026, 05, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Start)
            .WithErrorMessage("Start is required");
    }

    [Fact]
    public void Should_Have_Error_When_End_Is_Empty()
    {
        var input = new SalesReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = default
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.End)
            .WithErrorMessage("End is required");
    }

    [Fact]
    public void Should_Have_Error_When_Start_Is_Greater_Than_End()
    {
        var input = new SalesReportInput
        {
            Start = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor("Period")
            .WithErrorMessage("Start must be less than or equal to End");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Start_Equals_End()
    {
        var sameDate = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc);

        var input = new SalesReportInput
        {
            Start = sameDate,
            End = sameDate
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Input_Is_Valid()
    {
        var input = new SalesReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Return_Multiple_Errors_When_Start_And_End_Are_Empty()
    {
        var input = new SalesReportInput
        {
            Start = default,
            End = default
        };

        var result = _validator.TestValidate(input);

        result.ShouldHaveValidationErrorFor(x => x.Start);
        result.ShouldHaveValidationErrorFor(x => x.End);
    }

    [Fact]
    public void Should_Not_Have_Period_Error_When_Dates_Are_Valid()
    {
        var input = new SalesReportInput
        {
            Start = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }
}
using AquaGas.Plan.Domain.Enums;
using AquaGas.Report.Application.Dtos.Requests;
using AquaGas.Report.Application.Validators;
using FluentValidation.TestHelper;

namespace AquaGas.Report.Application.Tests.Validators;

public sealed class ContractPenaltyReportInputValidatorTests
{
    private readonly ContractPenaltyReportInputValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Start_Is_Greater_Than_End()
    {
        var input = new ContractPenaltyReportInput
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
        var sameDate = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc);

        var input = new ContractPenaltyReportInput
        {
            Start = sameDate,
            End = sameDate
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Start_Is_Null()
    {
        var input = new ContractPenaltyReportInput
        {
            Start = null,
            End = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Error_When_End_Is_Null()
    {
        var input = new ContractPenaltyReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = null
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Both_Dates_Are_Null()
    {
        var input = new ContractPenaltyReportInput
        {
            Start = null,
            End = null
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveValidationErrorFor("Period");
    }

    [Fact]
    public void Should_Not_Have_Any_Error_When_Input_Is_Valid()
    {
        var input = new ContractPenaltyReportInput
        {
            Start = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
        };

        var result = _validator.TestValidate(input);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
using AquaGas.Report.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Report.Application.Validators;

public sealed class SalesReportInputValidator
    : AbstractValidator<SalesReportInput>
{
    public SalesReportInputValidator()
    {
        RuleFor(x => x.Start)
            .NotEmpty()
            .WithMessage("Start is required")
            .OverridePropertyName("Start");

        RuleFor(x => x.End)
            .NotEmpty()
            .WithMessage("End is required")
            .OverridePropertyName("End");

        RuleFor(x => x)
            .Must(x => x.Start <= x.End)
            .WithMessage("Start must be less than or equal to End")
            .OverridePropertyName("Period");
    }
}

using AquaGas.Report.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Report.Application.Validators;

public sealed class ContractPenaltyReportInputValidator
    : AbstractValidator<ContractPenaltyReportInput>
{
    public ContractPenaltyReportInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.Start.HasValue || !x.End.HasValue || x.Start.Value <= x.End.Value)
            .WithMessage("Start must be less than or equal to End")
            .OverridePropertyName("Period");
    }
}

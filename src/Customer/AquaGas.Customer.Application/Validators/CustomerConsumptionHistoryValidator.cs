using AquaGas.Customer.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Customer.Application.Validators;

public sealed class CustomerConsumptionHistoryValidator
    : AbstractValidator<CustomerConsumptionHistoryInput>
{
    public CustomerConsumptionHistoryValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("StartDate is required")
            .OverridePropertyName("StartDate");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("EndDate is required")
            .OverridePropertyName("EndDate");

        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .WithMessage("StartDate must be less than or equal to EndDate")
            .OverridePropertyName("Period");
    }
}

using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class SuspendPlanInputValidator
    : AbstractValidator<SuspendPlanInput>
{
    public SuspendPlanInputValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(5)
            .WithMessage("Reason must have at least 5 characters")
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");
    }
}
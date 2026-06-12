using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class WaiveContractPenaltyInputValidator 
    : AbstractValidator<WaiveContractPenaltyInput>
{
    public WaiveContractPenaltyInputValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must contain at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}
using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class CancelContractPenaltyInputValidator : AbstractValidator<CancelContractPenaltyInput>
{
    public CancelContractPenaltyInputValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters long.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

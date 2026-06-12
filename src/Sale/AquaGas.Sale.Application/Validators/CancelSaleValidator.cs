using AquaGas.Sale.Application.Dtos.Requests;

using FluentValidation;

namespace AquaGas.Sale.Application.Validations;

public sealed class CancelSaleValidator
    : AbstractValidator<CancelSaleInput>
{
    public CancelSaleValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")

            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Reason cannot contain only spaces")

            .MinimumLength(2)
            .WithMessage(
                "Reason must contain at least 2 characters")

            .MaximumLength(500)
            .WithMessage(
                "Reason must contain at most 500 characters");
    }
}
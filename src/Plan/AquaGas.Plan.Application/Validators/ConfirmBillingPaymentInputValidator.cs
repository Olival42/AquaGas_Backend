using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class ConfirmBillingPaymentInputValidator
    : AbstractValidator<ConfirmBillingPaymentInput>
{
    public ConfirmBillingPaymentInputValidator()
    {
        RuleFor(x => x.BillingId)
            .NotEmpty()
            .WithMessage("BillingId is required");
    }
}
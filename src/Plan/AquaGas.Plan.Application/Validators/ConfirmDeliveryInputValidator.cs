using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class ConfirmDeliveryInputValidator
    : AbstractValidator<ConfirmDeliveryInput>
{
    public ConfirmDeliveryInputValidator()
    {
        RuleFor(x => x.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId is required");
    }
}
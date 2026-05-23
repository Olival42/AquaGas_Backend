using AquaGas.Plan.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class RescheduleDeliveryInputValidator
    : AbstractValidator<RescheduleDeliveryInput>
{
    public RescheduleDeliveryInputValidator()
    {
        RuleFor(x => x.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId is required");

        RuleFor(x => x.NewDate)
            .NotEmpty()
            .WithMessage("NewDate is required")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("NewDate must be in the future");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(5)
            .WithMessage("Reason must have at least 5 characters")
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");
    }
}
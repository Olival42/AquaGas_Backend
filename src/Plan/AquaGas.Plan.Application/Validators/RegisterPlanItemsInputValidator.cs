using AquaGas.Plan.Application.Dtos.Requests;

using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class RegisterPlanItemsInputValidator
    : AbstractValidator<PlanItemsInput>
{
    public RegisterPlanItemsInputValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");
    }
}
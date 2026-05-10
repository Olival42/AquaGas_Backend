using AquaGas.Product.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Product.Application.Validators;

public class RegisterProductValidator : AbstractValidator<RegisterProductInput>
{
    public RegisterProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .OverridePropertyName("Name");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .OverridePropertyName("Type");

        RuleFor(x => x.Price)
            .NotNull().WithMessage("Price is required")
            .OverridePropertyName("Price");

        RuleFor(x => x.Quantity)
            .NotNull().WithMessage("Quantity is required")
            .OverridePropertyName("Quantity");
    }
}
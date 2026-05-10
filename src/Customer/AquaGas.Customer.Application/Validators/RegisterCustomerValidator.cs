using AquaGas.Customer.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Customer.Application.Validators;

public class RegisterCustomerValidator : AbstractValidator<RegisterCustomerInput>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .OverridePropertyName("Name");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Document is required")
            .OverridePropertyName("Document");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .OverridePropertyName("Email");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .OverridePropertyName("Phone");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required")
            .OverridePropertyName("Address");

        When(x => x.Address != null, () =>
        {
        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Street is required")
            .MinimumLength(3).WithMessage("Street must have at least 3 characters")
            .OverridePropertyName("Street");

        RuleFor(x => x.Address.Number)
            .NotEmpty().WithMessage("Number is required")
            .MaximumLength(10).WithMessage("Number too long")
            .OverridePropertyName("Number");

        RuleFor(x => x.Address.Neighborhood)
            .NotEmpty().WithMessage("Neighborhood is required")
            .MinimumLength(2).WithMessage("Neighborhood must have at least 2 characters")
            .OverridePropertyName("Neighborhood");

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("City is required")
            .MinimumLength(2).WithMessage("City must have at least 2 characters")
            .OverridePropertyName("City");

        RuleFor(x => x.Address.Cep)
            .NotEmpty().WithMessage("Cep is required")
            .OverridePropertyName("Cep");
        });
    }
}
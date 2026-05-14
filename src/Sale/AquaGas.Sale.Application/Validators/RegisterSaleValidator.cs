using AquaGas.Sale.Application.Dtos.Requests;

using FluentValidation;

namespace AquaGas.Sale.Application.Validators;

public class RegisterSaleValidator
    : AbstractValidator<RegisterSaleInput>
{
    public RegisterSaleValidator()
    {
        RuleFor(x => x.SaleItems)
            .NotNull()
            .WithMessage("Sale items are required");

        RuleFor(x => x.SaleItems)
            .NotEmpty()
            .WithMessage("Sale must contain at least one item");

        RuleFor(x => x.CustomerId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("Customer id must be a valid UUID when informed");

        RuleForEach(x => x.SaleItems)
            .SetValidator(new SaleItemsValidator());
    }
}

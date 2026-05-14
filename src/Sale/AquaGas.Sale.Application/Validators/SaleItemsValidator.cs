using AquaGas.Sale.Application.Dtos.Requests;

using FluentValidation;

namespace AquaGas.Sale.Application.Validators;

public class SaleItemsValidator
    : AbstractValidator<SaleItemsInput>
{
    public SaleItemsValidator()
    {
        RuleFor(x => x.ProductId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Product id is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Product id is invalid");

        RuleFor(x => x.Quantity)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");
    }
}
using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Api.Modules.Product.Application.Validators;

public class UpdateStockValidator : AbstractValidator<UpdateStockInput>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.Quantity)
            .NotNull().WithMessage("Quantity is required")
            .OverridePropertyName("Quantity");

        RuleFor(x => x.StockMovementType)
            .NotEmpty().WithMessage("StockMovementType is required")
            .OverridePropertyName("StockMovementType");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MinimumLength(3).WithMessage("Reason must have at least 3 characters")
            .OverridePropertyName("Reason");
    }
}
using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Domain.Enums;
using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class DowngradePlanInputValidator : AbstractValidator<DowngradePlanInput>
{
    public DowngradePlanInputValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Cycle.HasValue || x.Items is { Count: > 0 })
            .WithMessage("At least one downgrade change must be informed: Cycle or Items.");

        RuleFor(x => x.Cycle)
            .IsInEnum()
            .When(x => x.Cycle.HasValue)
            .WithMessage("Invalid plan cycle.");

        RuleFor(x => x.DurationInMonths)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.Cycle == PlanCycle.Custom)
            .WithMessage("Duration in months is required for a custom cycle and must be greater than zero.");

        RuleFor(x => x.DurationInMonths)
            .Null()
            .When(x => x.Cycle.HasValue && x.Cycle != PlanCycle.Custom)
            .WithMessage("Duration in months must be null when cycle is not custom.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .When(x => x.Items is not null)
            .WithMessage("Items list cannot be empty when provided.");

        RuleFor(x => x.Items)
            .Must(items => items!.GroupBy(i => i.ProductId).Count() == items!.Count)
            .When(x => x.Items is not null)
            .WithMessage("Duplicate products are not allowed.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("ProductId is required.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Quantity must be zero or greater.");
            })
            .When(x => x.Items is not null);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MinimumLength(5)
            .WithMessage("Reason must be at least 5 characters long.")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters.");
    }
}

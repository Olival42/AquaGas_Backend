using AquaGas.Plan.Application.Dtos.Requests;

using FluentValidation;

namespace AquaGas.Plan.Application.Validators;

public sealed class RegisterPlanInputValidator
    : AbstractValidator<RegisterPlanInput>
{
    public RegisterPlanInputValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required");

        RuleFor(x => x.Cycle)
            .NotEmpty()
            .WithMessage("Cycle is required");

        RuleFor(x => x.DeliveryDay)
            .InclusiveBetween(1, 31)
            .WithMessage(
                "DeliveryDay must be between 1 and 31");

        RuleFor(x => x.BillingDay)
            .InclusiveBetween(1, 31)
            .WithMessage(
                "BillingDay must be between 1 and 31");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage(
                "Plan must contain at least one item");

        RuleFor(x => x.Items)
            .Must(items =>
                items.Select(i => i.ProductId)
                    .Distinct()
                    .Count() == items.Count)
            .WithMessage("Duplicate products are not allowed in the plan items");

        RuleForEach(x => x.Items)
            .SetValidator(
                new RegisterPlanItemsInputValidator());

        When(
            x => x.Cycle == "Custom",
            () =>
            {
                RuleFor(x => x.DurationInMonths)
                    .NotNull()
                    .WithMessage(
                        "DurationInMonths is required for custom plans");

                RuleFor(x => x.DurationInMonths)
                    .InclusiveBetween(2, 60)
                    .WithMessage(
                        "DurationInMonths for custom plans must be between 2 and 60 months");
            });

        When(
            x => x.Cycle != "Custom",
            () =>
            {
                RuleFor(x => x.DurationInMonths)
                    .Null()
                    .WithMessage(
                        "DurationInMonths should only be sent for custom plans");
            });
    }
}
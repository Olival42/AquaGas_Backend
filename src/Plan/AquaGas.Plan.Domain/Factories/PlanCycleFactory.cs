namespace AquaGas.Plan.Domain.Factories;

using AquaGas.Plan.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

public static class PlanCycleFactory
{
    public static Result<PlanCycle> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result<PlanCycle>.Fail(
                Error.Validation(
                    "PlanCycle is required",
                    "PlanCycle"));
        }

        var value = input.Trim();
        return value switch
        {
            "Monthly"
                => Result<PlanCycle>
                    .Success(PlanCycle.Monthly),

            "Quarterly"
                => Result<PlanCycle>
                    .Success(PlanCycle.Quarterly),

            "Annual"
                => Result<PlanCycle>
                    .Success(PlanCycle.Annual),

            "Custom"
                => Result<PlanCycle>
                    .Success(PlanCycle.Custom),

            _ => Result<PlanCycle>.Fail(
                Error.Validation(
                    "Plan cycle is invalid. Allowed: Monthly, Quarterly, Annual, Custom",
                    "PlanCycle"))
        };
    }
}
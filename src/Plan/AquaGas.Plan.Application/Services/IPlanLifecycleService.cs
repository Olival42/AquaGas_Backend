namespace AquaGas.Plan.Application.Services;

public interface IPlanLifecycleService
{
    Task TryCompletePlanAsync(Guid planId);
}

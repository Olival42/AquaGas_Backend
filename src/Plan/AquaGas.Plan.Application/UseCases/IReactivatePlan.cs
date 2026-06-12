using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface IReactivatePlan
{
    Task<Result<ReactivatePlanResponse>> Execute(Guid planId);
}

using AquaGas.Shared.Results;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Plan.Application.Dtos.Requests;

namespace AquaGas.Plan.Application.UseCases;

public interface ICancelPlan
{
    Task<Result<CancelPlanResponse>> Execute(Guid planId, CancelPlanInput input);
}
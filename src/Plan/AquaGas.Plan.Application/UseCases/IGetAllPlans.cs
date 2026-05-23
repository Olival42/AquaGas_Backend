using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface IGetAllPlans
{
    Task<Result<List<PlanResponse>>> Execute();
}
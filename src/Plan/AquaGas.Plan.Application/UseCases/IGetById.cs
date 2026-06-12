using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface IGetById
{
    Task<Result<PlanResponse>> Execute(Guid id);
}
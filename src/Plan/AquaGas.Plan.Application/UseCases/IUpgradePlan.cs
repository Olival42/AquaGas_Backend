using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface IUpgradePlan
{
    Task<Result<UpgradePlanResponse>> Execute(Guid id, UpgradePlanInput input);
}

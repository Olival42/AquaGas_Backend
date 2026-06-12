using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface ICancelContractPenalty
{
    Task<Result<CancelContractPenaltyResponse>> Execute(Guid id, CancelContractPenaltyInput input);
}

using AquaGas.Plan.Application.Dtos.Requests;
using AquaGas.Plan.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Plan.Application.UseCases;

public interface IConfirmBillingPayment
{
    Task<Result<ConfirmBillingPaymentResponse>> Execute(ConfirmBillingPaymentInput input);
}
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface ICustomerConsumptionHistory
{
    Task<Result<CustomerConsumptionResponse>> Execute(
        Guid id,
        CustomerConsumptionHistoryInput input);
}

using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface IGetByCustomerId
{
    Task<Result<CustomerResponse>> Execute(Guid id);
}
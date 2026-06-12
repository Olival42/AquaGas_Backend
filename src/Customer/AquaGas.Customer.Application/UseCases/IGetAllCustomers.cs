using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface IGetAllCustomers
{
    Task<Result<List<CustomerResponse>>> Execute();
}
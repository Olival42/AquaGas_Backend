using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

public interface IGetAllCustomers
{
    Task<Result<List<CustomerResponse>>> Execute();
}
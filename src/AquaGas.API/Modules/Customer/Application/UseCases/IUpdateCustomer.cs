using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

public interface IUpdateCustomer
{
    Task<Result<CustomerResponse>> Execute(UpdateCustomerInput data, Guid id);
}
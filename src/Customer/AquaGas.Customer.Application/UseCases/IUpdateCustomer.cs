using AquaGas.Shared.Results;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;

namespace AquaGas.Customer.Application.UseCases;

public interface IUpdateCustomer
{
    Task<Result<CustomerResponse>> Execute(UpdateCustomerInput data, Guid id);
}
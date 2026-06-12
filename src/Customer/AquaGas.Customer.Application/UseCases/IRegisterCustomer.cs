using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface IRegisterCustomer
{
    Task<Result<CustomerResponse>> Execute(RegisterCustomerInput data);
}
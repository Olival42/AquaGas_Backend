using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Customer.Application.UseCases;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Mapster;

namespace AquaGas.Customer.Application.UseCases;

public class GetByCustomerId : IGetByCustomerId
{
    private readonly ICustomerRepository _customerRepository;

    public GetByCustomerId(
        ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerResponse>> Execute(Guid id)
    {
        var customerEntity = await _customerRepository.GetByIdAsync(id);

        if (customerEntity is null)
            return Result<CustomerResponse>.Fail(
                Error.NotFound("Customer not found"));

        var customerDto = customerEntity.Adapt<CustomerResponse>();

        return Result<CustomerResponse>.Success(customerDto);
    }
}
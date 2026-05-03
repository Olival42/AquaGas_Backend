using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Modules.Customer.Application.UseCases;
using AquaGas.Api.Modules.Customer.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using Mapster;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

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
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Modules.Customer.Domain.Repositories;
using AquaGas.Api.Shared.Results;
using Mapster;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

public class GetAllCustomers : IGetAllCustomers
{
    private readonly ICustomerRepository _customerRepository;

    public GetAllCustomers(
        ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<List<CustomerResponse>>> Execute()
    {
        var customers = await _customerRepository.GetAllAsync();

        var activeCustomers = customers
            .Where(c => c.IsActive)
            .ToList();

        var response = activeCustomers.Adapt<List<CustomerResponse>>();

        return Result<List<CustomerResponse>>.Success(response);
    }
}
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Shared.Results;
using Mapster;

namespace AquaGas.Customer.Application.UseCases;

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
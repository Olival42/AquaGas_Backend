using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public class ExistsCustomerByDocument : IExistsCustomerByDocument
{
    private readonly ICustomerRepository _customerRepository;

    public ExistsCustomerByDocument(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<bool>> Execute(string document)
    {
        var documentResult = Document.Create(document);

        if (documentResult.IsFailure)
            return Result<bool>.Fail(documentResult.Errors.ToArray());

        var exists = await _customerRepository.AnyByDocumentAsync(documentResult.Value!.Value);

        return Result<bool>.Success(exists);
    }
}

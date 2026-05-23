using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface IExistsCustomerByDocument
{
    Task<Result<bool>> Execute(string document);
}

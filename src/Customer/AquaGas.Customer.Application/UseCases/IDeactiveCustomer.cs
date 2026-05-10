using AquaGas.Shared.Results;

namespace AquaGas.Customer.Application.UseCases;

public interface IDeactiveCustomer
{
    Task<Result<object>> Execute(Guid id);
}
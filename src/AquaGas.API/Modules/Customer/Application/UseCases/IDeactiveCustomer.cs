using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

public interface IDeactiveCustomer
{
    Task<Result<object>> Execute(Guid id);
}
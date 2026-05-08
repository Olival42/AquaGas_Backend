using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public interface IDeactiveProduct
{
    Task<Result<object>> Execute(Guid id);
}
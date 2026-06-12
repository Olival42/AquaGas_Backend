using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IDeactiveProduct
{
    Task<Result<object>> Execute(Guid id);
}
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IGetByProductId
{
    Task<Result<ProductResponse>> Execute(Guid id);
}
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IGetAllProducts
{
    Task<Result<List<ProductResponse>>> Execute();
}
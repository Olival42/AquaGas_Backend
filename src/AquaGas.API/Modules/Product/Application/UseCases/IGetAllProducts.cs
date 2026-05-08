using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public interface IGetAllProducts
{
    Task<Result<List<ProductResponse>>> Execute();
}
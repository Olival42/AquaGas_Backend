using AquaGas.Api.Modules.Product.Application.Dtos.Requests;
using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public interface IGetByProductId
{
    Task<Result<ProductResponse>> Execute(Guid id);
}
using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IUpdateStock
{
    Task<Result<ProductResponse>> Execute(UpdateStockInput data, Guid id);
}
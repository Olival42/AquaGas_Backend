using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IUpdateProduct
{
    Task<Result<ProductResponse>> Execute(UpdateProductInput data, Guid id);
}
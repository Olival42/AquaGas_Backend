using AquaGas.Product.Application.Dtos.Requests;
using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Product.Application.UseCases;

public interface IRegisterProduct
{
    Task<Result<ProductResponse>> Execute(RegisterProductInput data);
}
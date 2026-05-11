using AquaGas.Product.Application.Dtos.Responses;
using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Mapster;

namespace AquaGas.Product.Application.UseCases;

public class GetByProductId : IGetByProductId
{
    private readonly IProductRepository _productRepository;

    public GetByProductId(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductResponse>> Execute(Guid id)
    {
        var productEntity = await _productRepository.GetByIdAsync(id);

        if (productEntity is null)
            return Result<ProductResponse>.Fail(
                Error.NotFound("Product not found"));

        var productDto = productEntity.Adapt<ProductResponse>();

        return Result<ProductResponse>.Success(productDto);
    }
}
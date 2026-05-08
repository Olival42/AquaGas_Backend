using AquaGas.Api.Modules.Product.Application.Dtos.Responses;
using AquaGas.Api.Modules.Product.Domain.Repositories;
using AquaGas.Api.Shared.Results;
using Mapster;

namespace AquaGas.Api.Modules.Product.Application.UseCases;

public class GetAllProducts : IGetAllProducts
{
    private readonly IProductRepository _productRepository;

    public GetAllProducts(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<List<ProductResponse>>> Execute()
    {
        var products = await _productRepository.GetAllAsync();

        var activeProducts = products
            .Where(c => c.IsActive)
            .ToList();

        var response = activeProducts.Adapt<List<ProductResponse>>();

        return Result<List<ProductResponse>>.Success(response);
    }
}
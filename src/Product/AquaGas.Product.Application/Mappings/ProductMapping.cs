namespace AquaGas.Product.Application.Mappings;

using ProductEntity = AquaGas.Product.Domain.Models.Product;
using Mapster;
using AquaGas.Product.Application.Dtos.Resposes;
using AquaGas.Product.Application.Dtos.Responses;

public static class ProductMapping
{
    public static void Register()
    {
        TypeAdapterConfig<RegisterProductValidated, ProductEntity>
            .NewConfig()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Price, src => src.Price)
            .Map(dest => dest.Quantity, src => src.Quantity);

        TypeAdapterConfig<ProductEntity, ProductResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Price, src => src.Price.Value)
            .Map(dest => dest.Quantity, src => src.Quantity.Value);
    }
}
namespace AquaGas.Product.Application.Mappings;

using ProductEntity = AquaGas.Product.Domain.Models.Product;
using Mapster;
using AquaGas.Product.Application.Dtos.Resposes;
using AquaGas.Product.Application.Dtos.Responses;

public static class ProductMapping
{
    private static readonly Lock Sync = new();
    private static bool _globalRegistered;

    public static void Register(TypeAdapterConfig? config = null)
    {
        if (config is not null)
        {
            Apply(config);
            return;
        }

        lock (Sync)
        {
            if (_globalRegistered)
                return;

            Apply(TypeAdapterConfig.GlobalSettings);
            _globalRegistered = true;
        }
    }

    private static void Apply(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterProductValidated, ProductEntity>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Price, src => src.Price)
            .Map(dest => dest.Quantity, src => src.Quantity);

        config.NewConfig<ProductEntity, ProductResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Price, src => src.Price.Value)
            .Map(dest => dest.Quantity, src => src.Quantity.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}

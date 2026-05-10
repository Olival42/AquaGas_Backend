using AquaGas.Product.Application.Mappings;

namespace AquaGas.Product.Application;

public static class ProductModule
{
    public static void RegisterMappings()
    {
        ProductMapping.Register();
    }
}
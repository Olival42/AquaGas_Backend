using AquaGas.Customer.Application.Mappings;
using Mapster;

namespace AquaGas.Customer.Application;

public static class CustomerModule
{
    public static void RegisterMappings()
    {
        CustomerMapping.Register();
    }
}
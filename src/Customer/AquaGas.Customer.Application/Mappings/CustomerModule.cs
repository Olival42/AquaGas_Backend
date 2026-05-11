using AquaGas.Customer.Application.Mappings;

namespace AquaGas.Customer.Application;

public static class CustomerModule
{
    public static void RegisterMappings()
    {
        CustomerMapping.Register();
    }
}
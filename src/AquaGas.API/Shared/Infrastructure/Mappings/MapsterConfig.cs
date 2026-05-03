using AquaGas.API.Modules.Auth.Application.Mappings;
using AquaGas.API.Modules.Customer.Application.Mappings;
using AquaGas.API.Modules.Employee.Application.Mappings;

namespace AquaGas.Api.Shared.Infrastructure.Mappings;

public static class MapsterConfig
{

    public static void RegisterMappings()
	{
        UserMapping.Register();
        CustomerMapping.Register();
        EmployeeMapping.Register();
    }
}

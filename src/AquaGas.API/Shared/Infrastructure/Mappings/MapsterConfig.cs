using AquaGas.API.Modules.Auth.Application.Mappings;

namespace AquaGas.Api.Shared.Infrastructure.Mappings;

public static class MapsterConfig
{

    public static void RegisterMappings()
	{
        UserMapping.Register();
    }
}

namespace AquaGas.Auth.Application.Mappings;

using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Application.Dtos.Responses;
using Mapster;

public static class UserMapping
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
        config.NewConfig<User, UserResponse>()
            .Map(dest => dest.UserId, src => src.Id)
            .Map(dest => dest.UserName, src => src.UserName.Value)
            .Map(dest => dest.Role, src => src.Role);
    }
}

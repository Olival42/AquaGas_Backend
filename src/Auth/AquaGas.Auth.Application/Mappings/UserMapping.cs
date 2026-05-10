namespace AquaGas.Auth.Application.Mappings;

using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Application.Dtos.Responses;
using Mapster;

public static class UserMapping
{
    public static void Register()
    {
        TypeAdapterConfig<User, UserResponse>
            .NewConfig()
            .Map(dest => dest.UserId, src => src.Id)
            .Map(dest => dest.UserName, src => src.UserName.Value)
            .Map(dest => dest.Role, src => src.Role);
    }
}
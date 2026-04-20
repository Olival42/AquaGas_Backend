namespace AquaGas.API.Modules.Auth.Application.Mappings;

using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.Models;
using Mapster;

public static class UserMapping
{
    public static void Register()
    {
        TypeAdapterConfig<User, UserResponse>
            .NewConfig()
            .Map(dest => dest.UserId, src => src.Id)
            .Map(dest => dest.UserName, src => src.UserName.Value)
            .Map(dest => dest.Role, src => src.Role)
            .Map(dest => dest.EmployeeId, src => src.EmployeeId);
    }
}
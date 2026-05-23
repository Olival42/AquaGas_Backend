namespace AquaGas.Employee.Application.Mappings;

using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using Mapster;
using AquaGas.Employee.Application.Dtos.Responses;

public static class EmployeeMapping
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
        config.NewConfig<EmployeeEntity, EmployeeResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Cpf, src => src.CPF)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Phone, src => src.Phone)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}

namespace AquaGas.Employee.Application.Mappings;

using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;
using Mapster;
using AquaGas.Employee.Application.Dtos.Responses;

public static class EmployeeMapping
{
    public static void Register()
    {
        TypeAdapterConfig<EmployeeEntity, EmployeeResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Cpf, src => src.CPF)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Phone, src => src.Phone)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}
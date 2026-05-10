using AquaGas.Employee.Application.UseCases;

using AquaGas.Employee.Domain.Repositories;

using AquaGas.Employee.Infrastructure.Persistence;
using AquaGas.Employee.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Employee.Web.DependencyInjection;

public static class EmployeeDependencyInjection
{
    public static IServiceCollection AddEmployeeModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<EmployeeDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        services.AddScoped<IRegisterEmployee, RegisterEmployee>();
        services.AddScoped<IGetByIdEmployee, GetByIdEmployee>();
        services.AddScoped<IGetAllEmployees, GetAllEmployees>();
        services.AddScoped<IUpdateEmployee, UpdateEmployee>();
        services.AddScoped<IDeactiveEmployee, DeactiveEmployee>();

        return services;
    }
}
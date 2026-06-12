using AquaGas.Application.Services;
using AquaGas.Shared.Application.Repositories;

using AquaGas.Shared.Infrastructure.Persistence;
using AquaGas.Shared.Infrastructure.Persistence.Repositories;

using FluentValidation.AspNetCore;
using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AquaGas.Shared.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }

    public static IServiceCollection AddSharedRepositories(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    public static IServiceCollection AddSharedValidation(
        this IServiceCollection services,
        params Type[] validatorMarkers)
    {
        services.AddFluentValidationAutoValidation();

        foreach (var marker in validatorMarkers)
        {
            services.AddValidatorsFromAssemblyContaining(marker);
        }

        return services;
    }
}
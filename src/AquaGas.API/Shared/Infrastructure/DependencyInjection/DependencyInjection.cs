using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Application.UseCase;
using AquaGas.Api.Modules.Auth.Application.Validators;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Infrastructure.Repositories;
using AquaGas.Api.Modules.Auth.Infrastructure.Security.Services;
using AquaGas.Api.Shared.Infrastructure.Cache;
using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using AquaGas.API.Shared.Application.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Shared.Infrastructure.Persistence.Repositories;
using AquaGas.Api.Modules.Employee.Application.UseCases;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.API.Modules.Employee.Infrastructure.Persistence.Repositories;
using AquaGas.Api.Modules.Employee.Application.Validators;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Api.Shared.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ILogin, Login>();
        services.AddScoped<ILogout, Logout>();
        services.AddScoped<IRefresh, Refresh>();
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IRegisterEmployee, RegisterEmployee>();
        services.AddScoped<IGetByIdEmployee, GetByIdEmployee>();
        services.AddScoped<IGetAllEmployees, GetAllEmployees>();
        services.AddScoped<IDeactiveEmployee, DeactiveEmployee>();
        services.AddScoped<IUpdateEmployee, UpdateEmployee>();
        services.AddScoped<IResetPassword, ResetPassword>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddScoped<IRedisService, RedisService>();
        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginInputValidator>();
        services.AddValidatorsFromAssemblyContaining<RegisterEmployeeValidator>();
        return services;
    }
}

using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Application.UseCase;
using AquaGas.Api.Modules.Auth.Application.Validators;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Infrastructure.Repositories;
using AquaGas.Api.Modules.Auth.Infrastructure.Security.Services;
using AquaGas.Api.Modules.Auth.Infrastructure.Services;
using AquaGas.Api.Shared.Infrastructure.Cache;
using AquaGas.Api.Shared.Http;
using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using AquaGas.API.Shared.Application.Repositories;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Shared.Infrastructure.Persistence.Repositories;

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
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
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
        return services;
    }

    public static IServiceCollection AddStandardApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
                ErrorResponseHelper.BadRequestFromModelState(context.ModelState);
        });

        return services;
    }
}

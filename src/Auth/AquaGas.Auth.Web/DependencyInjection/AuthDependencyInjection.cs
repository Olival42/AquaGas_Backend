using AquaGas.Auth.Application.Services;

using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.Services;

using AquaGas.Auth.Infrastructure.Persistence;
using AquaGas.Auth.Infrastructure.Security.Services;

using AquaGas.Shared.Infrastructure.TokenBlacklist;
using AquaGas.Shared.Infrastructure.Cache;

using Microsoft.EntityFrameworkCore;
using AquaGas.Auth.Infrastructure.Repositories;
using AquaGas.Auth.Application.UseCase;
using AquaGas.Auth.Infrastructure.Services;
using AquaGas.Auth.Application.Validators;
using FluentValidation.AspNetCore;
using FluentValidation;

namespace AquaGas.Auth.Web.DependencyInjection;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUserContextService, UserContextService>();

        services.AddScoped<ILogin, Login>();
        services.AddScoped<ILogout, Logout>();
        services.AddScoped<IRefresh, Refresh>();

        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginInputValidator>();

        return services;
    }
}
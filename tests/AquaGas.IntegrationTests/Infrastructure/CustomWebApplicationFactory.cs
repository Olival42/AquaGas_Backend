extern alias Api;

using AquaGas.Auth.Domain.Services;
using AquaGas.Auth.Infrastructure.Persistence;
using AquaGas.Customer.Infrastructure.Persistence;
using AquaGas.Employee.Infrastructure.Persistence;
using AquaGas.Plan.Infrastructure.Persistence;
using AquaGas.Product.Infrastructure.Persistence;
using AquaGas.Sale.Infrastructure.Persistence;
using AquaGas.Shared.Infrastructure.Persistence;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using ApiProgram = Api::Program;

namespace AquaGas.IntegrationTests.Infrastructure;

// Startup filter to add cookie modification middleware
public class TestCookieStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Add our middleware first to modify cookies
            app.Use(async (context, nextMiddleware) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.Headers.ContainsKey("Set-Cookie"))
                    {
                        var originalCookies = context.Response.Headers["Set-Cookie"].ToList();
                        var modifiedCookies = new List<string>();
                        
                        foreach (var cookie in originalCookies)
                        {
                            // Remove Secure flag and adjust SameSite
                            var modifiedCookie = cookie
                                .Replace("; Secure", "")
                                .Replace("Secure; ", "")
                                .Replace("SameSite=Strict", "SameSite=Lax");
                            modifiedCookies.Add(modifiedCookie);
                        }
                        
                        context.Response.Headers["Set-Cookie"] = modifiedCookies.ToArray();
                    }
                    
                    return Task.CompletedTask;
                });
                
                await nextMiddleware(context);
            });
            
            // Run the rest of the app pipeline
            next(app);
        };
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<ApiProgram>, IAsyncLifetime
{
    private const string JwtTestKey = "IntegrationTestSecretKeyWith32CharsMin!!";
    private const string JwtIssuer = "AquaGas.API.Test";
    private const string JwtAudience = "AquaGas.Client.Test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("aquagas_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly IContainer _redis = new ContainerBuilder()
        .WithImage("redis:latest")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilCommandIsCompleted("redis-cli", "ping"))
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}",
                ["Jwt:Key"] = JwtTestKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Security:Argon2:TimeCost"] = "2",
                ["Security:Argon2:MemoryCost"] = "16384",
                ["Security:Argon2:Lanes"] = "4"
            });
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, TestCookieStartupFilter>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var authDb = sp.GetRequiredService<AuthDbContext>();
        var employeeDb = sp.GetRequiredService<EmployeeDbContext>();
        var customerDb = sp.GetRequiredService<CustomerDbContext>();
        var productDb = sp.GetRequiredService<ProductDbContext>();
        var saleDb = sp.GetRequiredService<SaleDbContext>();
        var planDb = sp.GetRequiredService<PlanDbContext>();
        var appDb = sp.GetRequiredService<AppDbContext>();

        await authDb.Database.MigrateAsync();
        await employeeDb.Database.MigrateAsync();
        await customerDb.Database.MigrateAsync();
        await productDb.Database.MigrateAsync();
        await saleDb.Database.MigrateAsync();
        await planDb.Database.MigrateAsync();
        await appDb.Database.MigrateAsync();

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(authDb, employeeDb, hasher);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Resolve all DbContexts
        var authDb = sp.GetRequiredService<AuthDbContext>();
        var employeeDb = sp.GetRequiredService<EmployeeDbContext>();
        var customerDb = sp.GetRequiredService<CustomerDbContext>();
        var productDb = sp.GetRequiredService<ProductDbContext>();
        var saleDb = sp.GetRequiredService<SaleDbContext>();
        var planDb = sp.GetRequiredService<PlanDbContext>();
        var appDb = sp.GetRequiredService<AppDbContext>();

        // Remove all data (order matters for foreign keys)
        await planDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Deliveries\", \"Billings\", \"PlanItems\", \"ContractPenalties\", \"Plans\" RESTART IDENTITY CASCADE;");
        await saleDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"SaleItems\", \"Sales\" RESTART IDENTITY CASCADE;");
        await productDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"StockMovements\", \"products\" RESTART IDENTITY CASCADE;");
        await customerDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Addresses\", \"Customers\" RESTART IDENTITY CASCADE;");
        await employeeDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Employees\" RESTART IDENTITY CASCADE;");
        await authDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"RefreshTokens\", \"Users\" RESTART IDENTITY CASCADE;");
        await appDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AuditLogs\" RESTART IDENTITY CASCADE;");

        // Re-seed the initial data
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(authDb, employeeDb, hasher);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}

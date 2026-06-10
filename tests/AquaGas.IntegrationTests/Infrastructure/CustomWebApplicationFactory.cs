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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using ApiProgram = Api::Program;

namespace AquaGas.IntegrationTests.Infrastructure;

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

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}

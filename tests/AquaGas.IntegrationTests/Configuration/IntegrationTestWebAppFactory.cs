using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;
using AquaGas.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AquaGas.IntegrationTests.Configuration;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly Dictionary<string, string?> _overrides;
    private readonly Action<IServiceCollection>? _serviceOverrides;
    private DatabaseResetHelper? _databaseResetHelper;

    public IntegrationTestWebAppFactory(
        string connectionString,
        Action<IServiceCollection>? serviceOverrides = null)
    {
        _connectionString = connectionString;
        _serviceOverrides = serviceOverrides;
        _overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["Jwt:Key"] = "integration-tests-super-secret-key-with-at-least-32-chars",
            ["Jwt:Issuer"] = "AquaGas.IntegrationTests",
            ["Jwt:Audience"] = "AquaGas.IntegrationTests.Client"
        };
    }

    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddJsonFile("appsettings.Test.json", optional: true);
            configBuilder.AddInMemoryCollection(_overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));

            services.RemoveAll<ITokenBlacklistService>();
            services.AddScoped<ITokenBlacklistService, NoOpTokenBlacklistService>();

            _serviceOverrides?.Invoke(services);
        });
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseReadyAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        _databaseResetHelper ??= new DatabaseResetHelper(_connectionString);
        await _databaseResetHelper.InitializeAsync();
        await _databaseResetHelper.ResetAsync();
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        _databaseResetHelper = new DatabaseResetHelper(_connectionString);
        await _databaseResetHelper.InitializeAsync();
    }
}

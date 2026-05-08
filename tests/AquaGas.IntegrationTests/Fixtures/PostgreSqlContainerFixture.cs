using AquaGas.IntegrationTests.Configuration;
using Testcontainers.PostgreSql;

namespace AquaGas.IntegrationTests.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("aquagas_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IntegrationTestWebAppFactory Factory { get; private set; } = null!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new IntegrationTestWebAppFactory(ConnectionString);
        await Factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        await _container.DisposeAsync();
    }
}

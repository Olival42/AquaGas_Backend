using Npgsql;
using Respawn;

namespace AquaGas.IntegrationTests.Helpers;

public sealed class DatabaseResetHelper
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Respawner? _respawner;

    public DatabaseResetHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_respawner is not null)
                return;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("DatabaseResetHelper must be initialized before reset.");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}

using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Satu Postgres untuk seluruh sweep.
public sealed class CrossCuttingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    public async Task<string> CreateFreshDatabaseAsync(string prefix)
    {
        var databaseName = $"wms_{prefix}_{Guid.NewGuid():N}";
        var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();
            }
        }

        return new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = databaseName,
        }.ConnectionString;
    }
}

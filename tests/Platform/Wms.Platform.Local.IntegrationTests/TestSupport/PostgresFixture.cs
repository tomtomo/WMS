using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Satu container Postgres dipakai bersama.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<string> CreateFreshDatabaseAsync()
    {
        var databaseName = "wms_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnection(_container.GetConnectionString());
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
        }.ConnectionString;
    }
}

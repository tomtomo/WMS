using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Postgres dipakai untuk dua peran: acuan bagi adapter Local, dan proxy untuk Flexible Server.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

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

        return new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = databaseName }.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

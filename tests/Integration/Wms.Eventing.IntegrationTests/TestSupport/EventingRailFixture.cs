using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Wms.Eventing.IntegrationTests.TestSupport;

// Menyiapkan PostgreSQL dan RabbitMQ untuk test eventing rail.
public sealed class EventingRailFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-alpine").Build();

    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
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

        return new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = databaseName,
        }.ConnectionString;
    }
}

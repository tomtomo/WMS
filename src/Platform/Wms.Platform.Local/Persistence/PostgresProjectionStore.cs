using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Persistence;

// Projection store Reporting. (Cosmos DB /Firestore di cloud).
public sealed class PostgresProjectionStore(NpgsqlDataSource dataSource, TimeProvider timeProvider)
    : IProjectionStore
{
    private const string EnsureTableSql = """
        CREATE SCHEMA IF NOT EXISTS infrastructure;
        CREATE TABLE IF NOT EXISTS infrastructure.projection (
            projection_type text NOT NULL,
            projection_key text NOT NULL,
            document jsonb NOT NULL,
            updated_at timestamptz NOT NULL,
            PRIMARY KEY (projection_type, projection_key));
        """;

    private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
    private volatile bool _bootstrapped;

    public async Task UpsertAsync<TProjection>(string key, TProjection projection, CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(projection);

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = dataSource.CreateCommand("""
            INSERT INTO infrastructure.projection (projection_type, projection_key, document, updated_at)
            VALUES (@type, @key, @document, @updatedAt)
            ON CONFLICT (projection_type, projection_key) DO UPDATE
                SET document = EXCLUDED.document, updated_at = EXCLUDED.updated_at
            """);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("type", ProjectionTypeName<TProjection>());
            command.Parameters.AddWithValue("key", key);
            command.Parameters.Add(new NpgsqlParameter("document", NpgsqlDbType.Jsonb)
            {
                Value = JsonSerializer.Serialize(projection),
            });
            command.Parameters.AddWithValue("updatedAt", timeProvider.GetUtcNow());
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<TProjection?> GetAsync<TProjection>(string key, CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = dataSource.CreateCommand(
            "SELECT document FROM infrastructure.projection WHERE projection_type = @type AND projection_key = @key LIMIT 1");
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("type", ProjectionTypeName<TProjection>());
            command.Parameters.AddWithValue("key", key);
            var document = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            return document is null ? null : JsonSerializer.Deserialize<TProjection>(document);
        }
    }

    private static string ProjectionTypeName<TProjection>() => typeof(TProjection).FullName ?? typeof(TProjection).Name;

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_bootstrapped)
        {
            return;
        }

        await _bootstrapLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bootstrapped)
            {
                return;
            }

            var command = dataSource.CreateCommand(EnsureTableSql);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            _bootstrapped = true;
        }
        finally
        {
            _bootstrapLock.Release();
        }
    }
}

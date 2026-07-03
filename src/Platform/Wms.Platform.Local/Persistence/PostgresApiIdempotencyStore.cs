using System.Globalization;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Persistence;

// Idempotency store REST di Postgres — mode Local (cloud: Managed Redis / Memorystore).
public sealed class PostgresApiIdempotencyStore(NpgsqlDataSource dataSource, TimeProvider timeProvider)
    : IApiIdempotencyStore
{
    private const string EnsureTableSql = """
        CREATE SCHEMA IF NOT EXISTS infrastructure;
        CREATE TABLE IF NOT EXISTS infrastructure.api_idempotency (
            idempotency_key text PRIMARY KEY,
            response text NOT NULL,
            expires_at timestamptz NOT NULL);
        """;

    private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
    private volatile bool _bootstrapped;

    public async Task<string?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = dataSource.CreateCommand(
            "SELECT response FROM infrastructure.api_idempotency WHERE idempotency_key = @key AND expires_at > @now LIMIT 1");
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", idempotencyKey);
            command.Parameters.AddWithValue("now", timeProvider.GetUtcNow());
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }
    }

    public async Task SaveResponseAsync(
        string idempotencyKey,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(response);
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "TTL idempotency wajib positif.");
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = dataSource.CreateCommand("""
            INSERT INTO infrastructure.api_idempotency AS existing (idempotency_key, response, expires_at)
            VALUES (@key, @response, @expiresAt)
            ON CONFLICT (idempotency_key) DO UPDATE
                SET response = EXCLUDED.response, expires_at = EXCLUDED.expires_at
                WHERE existing.expires_at <= @now
            """);
        await using (command.ConfigureAwait(false))
        {
            var now = timeProvider.GetUtcNow();
            command.Parameters.AddWithValue("key", idempotencyKey);
            command.Parameters.AddWithValue("response", response);
            command.Parameters.AddWithValue("expiresAt", now.Add(timeToLive));
            command.Parameters.AddWithValue("now", now);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

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

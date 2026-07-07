using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Persistence;

// Store idempotency berbasis Postgres untuk mode local.
// Klaim dibuat sebagai row pending, row expired boleh diambil alih, dengan owner_token sebagai fencing.
public sealed class PostgresApiIdempotencyStore(NpgsqlDataSource dataSource, TimeProvider timeProvider)
    : IApiIdempotencyStore
{
    private const string StatusPending = "pending";
    private const string StatusCompleted = "completed";

    // Upgrade schema tetap idempotent, data lama dianggap sudah completed.
    private const string EnsureTableSql = """
        CREATE SCHEMA IF NOT EXISTS infrastructure;
        CREATE TABLE IF NOT EXISTS infrastructure.api_idempotency (
            idempotency_key text PRIMARY KEY,
            status text NOT NULL,
            response text NULL,
            owner_token uuid NULL,
            expires_at timestamptz NOT NULL);
        ALTER TABLE infrastructure.api_idempotency ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'completed';
        ALTER TABLE infrastructure.api_idempotency ALTER COLUMN status DROP DEFAULT;
        ALTER TABLE infrastructure.api_idempotency ALTER COLUMN response DROP NOT NULL;
        ALTER TABLE infrastructure.api_idempotency ADD COLUMN IF NOT EXISTS owner_token uuid;
        """;

    private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
    private volatile bool _bootstrapped;

    public async Task<IdempotencyReservation> TryReserveAsync(
        string idempotencyKey,
        TimeSpan pendingTimeToLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (pendingTimeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pendingTimeToLive), "TTL klaim pending wajib positif.");
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        var ownerToken = Guid.NewGuid();

        var reserve = dataSource.CreateCommand("""
            INSERT INTO infrastructure.api_idempotency AS existing (idempotency_key, status, response, owner_token, expires_at)
            VALUES (@key, 'pending', NULL, @ownerToken, @pendingExpiresAt)
            ON CONFLICT (idempotency_key) DO UPDATE
                SET status = EXCLUDED.status, response = EXCLUDED.response,
                    owner_token = EXCLUDED.owner_token, expires_at = EXCLUDED.expires_at
                WHERE existing.expires_at <= @now
            """);
        await using (reserve.ConfigureAwait(false))
        {
            reserve.Parameters.AddWithValue("key", idempotencyKey);
            reserve.Parameters.AddWithValue("ownerToken", ownerToken);
            reserve.Parameters.AddWithValue("pendingExpiresAt", now.Add(pendingTimeToLive));
            reserve.Parameters.AddWithValue("now", now);
            var claimed = await reserve.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (claimed > 0)
            {
                return IdempotencyReservation.Reserved(ownerToken);
            }
        }

        // Klaim sudah dipegang request lain, cek apakah hasilnya sudah tersedia.
        var read = dataSource.CreateCommand(
            "SELECT status, response FROM infrastructure.api_idempotency WHERE idempotency_key = @key AND expires_at > @now LIMIT 1");
        await using (read.ConfigureAwait(false))
        {
            read.Parameters.AddWithValue("key", idempotencyKey);
            read.Parameters.AddWithValue("now", now);
            var reader = await read.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var status = reader.GetString(0);
                    if (status == StatusCompleted && !await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false))
                    {
                        return IdempotencyReservation.Completed(reader.GetString(1));
                    }
                }
            }
        }

        // Jika state berubah di antara query, biarkan retry berikutnya mencoba lagi.
        return IdempotencyReservation.Pending;
    }

    public async Task CompleteAsync(
        string idempotencyKey,
        Guid ownerToken,
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

        // Hanya pemilik klaim aktif yang boleh menyimpan hasil.
        var command = dataSource.CreateCommand("""
            UPDATE infrastructure.api_idempotency
            SET status = 'completed', response = @response, owner_token = NULL, expires_at = @expiresAt
            WHERE idempotency_key = @key AND status = 'pending' AND owner_token = @ownerToken
            """);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", idempotencyKey);
            command.Parameters.AddWithValue("ownerToken", ownerToken);
            command.Parameters.AddWithValue("response", response);
            command.Parameters.AddWithValue("expiresAt", timeProvider.GetUtcNow().Add(timeToLive));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = dataSource.CreateCommand("""
            DELETE FROM infrastructure.api_idempotency
            WHERE idempotency_key = @key AND status = @pending AND owner_token = @ownerToken
            """);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", idempotencyKey);
            command.Parameters.AddWithValue("pending", StatusPending);
            command.Parameters.AddWithValue("ownerToken", ownerToken);
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

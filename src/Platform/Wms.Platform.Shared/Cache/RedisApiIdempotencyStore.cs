using System.Globalization;
using StackExchange.Redis;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Cache;

// Redis menangani masa berlaku klaim secara otomatis, jadi klaim yang tertahan akan hilang tanpa purge job.
// Owner token memastikan hanya pemilik klaim yang masih valid yang dapat menyimpan hasil akhirnya.
public sealed class RedisApiIdempotencyStore(IConnectionMultiplexer multiplexer) : IApiIdempotencyStore
{
    private const string PendingPrefix = "pending:";
    private const string CompletedPrefix = "completed:";

    // Gunakan script Lua agar pemeriksaan owner token dan update data berjalan sebagai satu operasi.
    private const string CompleteIfOwnerScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('set', KEYS[1], ARGV[2], 'PX', ARGV[3])
        end
        return nil
        """;

    private const string ReleaseIfOwnerScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        end
        return 0
        """;

    private IDatabase Database => multiplexer.GetDatabase();

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

        var ownerToken = Guid.NewGuid();
        var claimed = await Database
            .StringSetAsync(idempotencyKey, PendingValue(ownerToken), pendingTimeToLive, When.NotExists)
            .ConfigureAwait(false);
        if (claimed)
        {
            return IdempotencyReservation.Reserved(ownerToken);
        }

        var current = await Database.StringGetAsync(idempotencyKey).ConfigureAwait(false);
        if (current.IsNullOrEmpty)
        {
            // Klaim sudah kedaluwarsa sebelum data sempat dibaca, jadi request berikutnya dapat mengambil alih.
            return IdempotencyReservation.Pending;
        }

        var value = current.ToString();
        return value.StartsWith(CompletedPrefix, StringComparison.Ordinal)
            ? IdempotencyReservation.Completed(value[CompletedPrefix.Length..])
            : IdempotencyReservation.Pending;
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

        await Database.ScriptEvaluateAsync(
                CompleteIfOwnerScript,
                [idempotencyKey],
                [
                    PendingValue(ownerToken),
                    CompletedPrefix + response,
                    ((long)timeToLive.TotalMilliseconds).ToString(CultureInfo.InvariantCulture),
                ])
            .ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        await Database
            .ScriptEvaluateAsync(ReleaseIfOwnerScript, [idempotencyKey], [PendingValue(ownerToken)])
            .ConfigureAwait(false);
    }

    private static string PendingValue(Guid ownerToken) => PendingPrefix + ownerToken.ToString("N", CultureInfo.InvariantCulture);
}

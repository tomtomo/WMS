using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Double reserve-then-fill in-memory; TTL diabaikan (test tak butuh clock). Equality record entry
// menjadikan TryUpdate/TryRemove conditional — setia pada guard status+owner_token Postgres.
internal sealed class InMemoryIdempotencyStore : IApiIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new(StringComparer.Ordinal);

    public Task<IdempotencyReservation> TryReserveAsync(
        string idempotencyKey,
        TimeSpan pendingTimeToLive,
        CancellationToken cancellationToken = default)
    {
        var ownerToken = Guid.NewGuid();
        if (_entries.TryAdd(idempotencyKey, IdempotencyEntry.Pending(ownerToken)))
        {
            return Task.FromResult(IdempotencyReservation.Reserved(ownerToken));
        }

        return Task.FromResult(
            _entries.TryGetValue(idempotencyKey, out var entry) && entry.Response is not null
                ? IdempotencyReservation.Completed(entry.Response)
                : IdempotencyReservation.Pending);
    }

    public Task CompleteAsync(
        string idempotencyKey,
        Guid ownerToken,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        _entries.TryUpdate(idempotencyKey, IdempotencyEntry.Completed(response), IdempotencyEntry.Pending(ownerToken));
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(KeyValuePair.Create(idempotencyKey, IdempotencyEntry.Pending(ownerToken)));
        return Task.CompletedTask;
    }

    private sealed record IdempotencyEntry(string? Response, Guid OwnerToken)
    {
        public static IdempotencyEntry Pending(Guid ownerToken) => new(null, ownerToken);

        public static IdempotencyEntry Completed(string response) => new(response, Guid.Empty);
    }
}

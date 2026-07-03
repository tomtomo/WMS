using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Cache;

// Cache aside Local (cloud: Managed Redis / Memorystore).
public sealed class InMemoryCacheStore(TimeProvider timeProvider) : ICacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<T?>(default);
        }

        if (entry.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(entry.Value is T typed ? typed : default(T?));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is null || timeToLive <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        _entries[key] = new CacheEntry(value, timeProvider.GetUtcNow().Add(timeToLive));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAt);
}

using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.IntegrationTests.TestSupport;

internal sealed class InMemoryApiIdempotencyStore : IApiIdempotencyStore
{
    private readonly ConcurrentDictionary<string, string> _store = new(StringComparer.Ordinal);

    public Task<string?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(idempotencyKey, out var value) ? value : null);

    public Task SaveResponseAsync(
        string idempotencyKey,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        _store[idempotencyKey] = response;
        return Task.CompletedTask;
    }
}

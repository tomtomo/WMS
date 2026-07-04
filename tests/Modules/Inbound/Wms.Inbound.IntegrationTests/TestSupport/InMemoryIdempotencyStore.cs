using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inbound.IntegrationTests.TestSupport;

internal sealed class InMemoryIdempotencyStore : IApiIdempotencyStore
{
    private readonly ConcurrentDictionary<string, string> _responses = new(StringComparer.Ordinal);

    public Task<string?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_responses.TryGetValue(idempotencyKey, out var response) ? response : null);

    public Task SaveResponseAsync(
        string idempotencyKey,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        _responses.TryAdd(idempotencyKey, response);
        return Task.CompletedTask;
    }
}

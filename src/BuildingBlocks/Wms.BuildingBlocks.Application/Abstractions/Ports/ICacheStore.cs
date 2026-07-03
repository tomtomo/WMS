namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Cache-aside store: InMemory Local, Managed Redis Azure, Memorystore GCP.
public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

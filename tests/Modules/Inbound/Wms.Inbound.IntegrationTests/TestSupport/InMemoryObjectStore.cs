using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Double IObjectStore
internal sealed class InMemoryObjectStore : IObjectStore
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _objects = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, (byte[] Bytes, string ContentType)> Objects => _objects;

    public async Task PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _objects[path] = (buffer.ToArray(), contentType);
    }

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default) =>
        _objects.TryGetValue(path, out var stored)
            ? Task.FromResult<Stream>(new MemoryStream(stored.Bytes))
            : throw new FileNotFoundException($"Objek '{path}' tidak ditemukan.");

    public Uri CreateReadUrl(string path, TimeSpan timeToLive) =>
        new($"https://objectstore.test/{path}?expires={(long)timeToLive.TotalSeconds}&sig=test-signature");
}

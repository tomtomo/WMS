namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Port — object storage untuk menyimpan dan mengambil objek biner.
public interface IObjectStore
{
    Task PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default);

    Uri CreateReadUrl(string path, TimeSpan timeToLive);
}

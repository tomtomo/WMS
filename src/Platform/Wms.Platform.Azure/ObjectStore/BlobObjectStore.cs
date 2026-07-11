using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.ObjectStore;

// Simpan file di Blob Storage dan berikan URL sementara agar file didownload langsung tanpa melewati service.
// URL dibatasi untuk satu blob dan dibuat dengan Managed Identity tanpa account key.
public sealed class BlobObjectStore : IObjectStore
{
    private readonly BlobContainerClient _container;
    private readonly UserDelegationKeyCache _delegationKeys;
    private readonly TimeProvider _timeProvider;
    private readonly BlobObjectStoreOptions _options;

    public BlobObjectStore(BlobServiceClient serviceClient, IOptions<BlobObjectStoreOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceClient);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _timeProvider = timeProvider;
        _container = serviceClient.GetBlobContainerClient(_options.ContainerName);
        _delegationKeys = new UserDelegationKeyCache(
            serviceClient,
            timeProvider,
            _options.UserDelegationKeyLifetime,
            _options.ClockSkew);
    }

    public async Task PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var blob = _container.GetBlobClient(GuardPath(path));
        await blob
            .UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(GuardPath(path));
        try
        {
            return await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException notFound) when (notFound.Status == 404)
        {
            // Samakan dengan perilaku filesystem di Local agar pemanggil cukup menangani FileNotFoundException.
            throw new FileNotFoundException($"Objek '{path}' tidak ditemukan.", path, notFound);
        }
    }

    public Uri CreateReadUrl(string path, TimeSpan timeToLive)
    {
        var blobPath = GuardPath(path);
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "TTL valet-key wajib positif.");
        }

        var now = _timeProvider.GetUtcNow();
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,

            // Batasi URL hanya untuk blob ini, bukan seluruh container.
            BlobName = blobPath,
            Resource = "b",
            StartsOn = now - _options.ClockSkew,
            ExpiresOn = now + timeToLive,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var accountName = _container.AccountName;
        var signature = sasBuilder.ToSasQueryParameters(_delegationKeys.Get(), accountName);

        return new UriBuilder(_container.GetBlobClient(blobPath).Uri) { Query = signature.ToString() }.Uri;
    }

    // Samakan validasi dengan Local: path harus relatif, memakai pemisah '/', dan tidak boleh berisi navigasi direktori.
    // Menjaga format {grId}/{attachmentId}/{fileName} tetap valid.
    private static string GuardPath(string blobPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        if (blobPath.Contains('\\', StringComparison.Ordinal)
            || blobPath.Contains(':', StringComparison.Ordinal)
            || blobPath.StartsWith('/'))
        {
            throw new ArgumentException($"blobPath '{blobPath}' ditolak: wajib relatif bersegmen '/'.", nameof(blobPath));
        }

        var segments = blobPath.Split('/');
        return segments.Any(segment => segment.Length == 0 || segment is "." or "..")
            ? throw new ArgumentException(
                $"blobPath '{blobPath}' ditolak: segmen kosong atau navigasi direktori.",
                nameof(blobPath))
            : blobPath;
    }
}

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Wms.Platform.Azure.Enrichment;

namespace Wms.Scheduled.Functions.Azure;

// Azure Function membaca attachment dari Blob Storage, menghitung SHA-256, lalu menyimpannya sebagai metadata.
// Jika metadata sha256 sudah ada, file dilewati agar pengiriman ulang tidak memproses attachment yang sama.
public sealed class AttachmentEnrichWorkerFunction(
    ILogger<AttachmentEnrichWorkerFunction> logger,
    TimeProvider timeProvider)
{
    [Function("AttachmentEnrichWorker")]
    public async Task RunAsync(
        [QueueTrigger(AttachmentPipeline.Queue, Connection = AttachmentPipeline.StorageConnection)]
        AttachmentEnrichmentMessage message,
        [BlobInput(AttachmentPipeline.Container + "/{BlobName}", Connection = AttachmentPipeline.StorageConnection)]
        BlobClient blob,
        CancellationToken cancellationToken)
    {
        Response<BlobProperties> properties;
        try
        {
            properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Blob sudah terhapus sebelum Azure Function memprosesnya, jadi enrichment dilewati.
            logger.LogWarning(ex, "Blob {BlobName} tak ditemukan saat enrichment.", message.BlobName);
            return;
        }

        if (AttachmentEnricher.IsAlreadyEnriched(properties.Value.Metadata))
        {
            logger.LogInformation("Lampiran {BlobName} sudah terenrich", message.BlobName);
            return;
        }

        var content = await blob.DownloadContentAsync(cancellationToken);
        var enriched = AttachmentEnricher.BuildEnrichmentMetadata(
            content.Value.Content.ToMemory().Span,
            properties.Value.Metadata,
            timeProvider.GetUtcNow());

        // value enriched seharusnya selalu tersedia karena pengecekan sebelumnya, guard ini hanya untuk analyzer.
        if (enriched is null)
        {
            return;
        }

        await blob.SetMetadataAsync(enriched, cancellationToken: cancellationToken);
        logger.LogInformation("Metadata checksum ditulis untuk lampiran {BlobName}.", message.BlobName);
    }
}

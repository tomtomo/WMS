using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Wms.Platform.Azure.Enrichment;

namespace Wms.Scheduled.Functions.Azure;

// Terima event BlobCreated dari Event Grid untuk container gr-attachments, lalu masukkan lampirannya ke Azure Queue Storage.
// Subscription ini terpisah dari FileIngestionFunction agar event tidak diproses dua kali.
public sealed class AttachmentBlobCreatedFunction(ILogger<AttachmentBlobCreatedFunction> logger)
{
    [Function("AttachmentBlobCreated")]
    [QueueOutput(AttachmentPipeline.Queue, Connection = AttachmentPipeline.StorageConnection)]
    public AttachmentEnrichmentMessage? Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        var blobName = AttachmentEnricher.TryExtractBlobName(cloudEvent.Subject, AttachmentPipeline.Container);
        if (blobName is null)
        {
            logger.LogWarning(
                "BlobCreated subject di luar container {Container}: {Subject} — dilewati.",
                AttachmentPipeline.Container,
                cloudEvent.Subject);
            return null;
        }

        logger.LogInformation("Antre enrichment lampiran {BlobName}.", blobName);
        return new AttachmentEnrichmentMessage(blobName);
    }
}

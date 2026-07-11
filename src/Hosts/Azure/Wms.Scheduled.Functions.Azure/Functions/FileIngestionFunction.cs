using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Wms.Scheduled.Functions.Azure;

// Trigger ini menerima event BlobCreated dari Event Grid untuk alur file ingestion.
// Handler untuk memproses file dan membuat GR atau Order belum tersedia, jadi saat ini event hanya dicatat.
public sealed class FileIngestionFunction(ILogger<FileIngestionFunction> logger)
{
    [Function("FileIngestionDropped")]
    public Task RunAsync([EventGridTrigger] CloudEvent cloudEvent)
    {
        logger.LogInformation(
            "File-ingestion seam: {EventType} untuk {Subject} diterima.",
            cloudEvent.Type,
            cloudEvent.Subject);
        return Task.CompletedTask;
    }
}

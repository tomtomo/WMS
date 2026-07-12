using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Wms.Scheduled.Functions.Azure;

// Trigger ini menerima event BlobCreated dari Event Grid untuk proses file ingestion.
// Karena handler pembuatan GR atau Order belum tersedia, event untuk sementara hanya dicatat.
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

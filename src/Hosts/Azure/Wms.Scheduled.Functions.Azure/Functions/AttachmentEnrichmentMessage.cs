namespace Wms.Scheduled.Functions.Azure;

// Message antar Azure Function yang membawa nama blob dari container gr attachments.
public sealed record AttachmentEnrichmentMessage(string BlobName);

// Konstanta binding Azure Queue Storage dan Event Grid untuk pipeline enrichment lampiran.
internal static class AttachmentPipeline
{
    internal const string Container = "gr-attachments";
    internal const string Queue = "attachment-enrich";
    internal const string StorageConnection = "Storage";
}

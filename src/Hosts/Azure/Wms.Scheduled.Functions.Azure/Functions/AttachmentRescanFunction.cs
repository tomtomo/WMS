using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Wms.Scheduled.Functions.Azure;

// Endpoint untuk memasukkan ulang satu lampiran ke antrean enrichment secara manual.
// Aksesnya dilindungi dengan function key.
public sealed class AttachmentRescanFunction(ILogger<AttachmentRescanFunction> logger)
{
    [Function("AttachmentRescan")]
    public async Task<RescanOutput> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "attachments/rescan")] HttpRequestData request)
    {
        var blobName = request.Query["blobName"];
        if (string.IsNullOrWhiteSpace(blobName))
        {
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Query 'blobName' wajib diisi.");
            return new RescanOutput { HttpResponse = badRequest };
        }

        logger.LogInformation("Rescan manual lampiran {BlobName}.", blobName);
        var accepted = request.CreateResponse(HttpStatusCode.Accepted);
        return new RescanOutput
        {
            Message = new AttachmentEnrichmentMessage(blobName),
            HttpResponse = accepted,
        };
    }
}

// Mengembalikan respons HTTP sekaligus pesan enrichment.
// Jika Message null, request tidak dimasukkan ke antrean.
public sealed class RescanOutput
{
    [QueueOutput(AttachmentPipeline.Queue, Connection = AttachmentPipeline.StorageConnection)]
    public AttachmentEnrichmentMessage? Message { get; set; }

    public HttpResponseData HttpResponse { get; set; } = default!;
}

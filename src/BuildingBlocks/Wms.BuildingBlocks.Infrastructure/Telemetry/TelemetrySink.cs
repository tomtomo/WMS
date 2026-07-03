using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.Telemetry;

// Kegagalan backend tidak boleh melempar maupun menjadi Failure bisnis. Emit lewat ILogger yang ambil pipeline OTel.
public sealed class TelemetrySink(ILogger<TelemetrySink> logger) : ITelemetrySink
{
    public Task RecordAsync(
        string name,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (logger.BeginScope(tags))
            {
                logger.LogInformation("Telemetry {TelemetryName}", name);
            }
        }
#pragma warning disable RCS1075
        catch (Exception)
        {
            // Empty catch
        }
#pragma warning restore RCS1075

        return Task.CompletedTask;
    }
}

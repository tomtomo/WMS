using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Telemetry;

// Telemetry Azure tetap memakai OpenTelemetry, dengan Application Insights sebagai exporter.
// Kegagalan telemetry tidak boleh mengganggu proses bisnis.
public sealed class AppInsightsTelemetrySink(
    ActivitySource activitySource,
    ILogger<AppInsightsTelemetrySink> logger) : ITelemetrySink
{
    public Task RecordAsync(
        string name,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        try
        {
            using var activity = activitySource.StartActivity(name, ActivityKind.Internal);
            foreach (var tag in tags)
            {
                activity?.SetTag(tag.Key, tag.Value);
            }

            using (logger.BeginScope(tags))
            {
                logger.LogInformation("Telemetry {TelemetryName}", name);
            }
        }
#pragma warning disable CA1031, RCS1075
        catch (Exception)
        {
            // Kegagalan telemetry bukan kegagalan bisnis.
        }
#pragma warning restore CA1031, RCS1075

        return Task.CompletedTask;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Analytics;

// Dev: sink OLAP berbentuk log baris datar. Real = BigQuery (GCP) / Synapse-Fabric (Azure)
public sealed class LogCsvAnalyticsSink(ILogger<LogCsvAnalyticsSink> logger) : IAnalyticsSink
{
    public Task EmitAsync<TEvent>(TEvent analyticsEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(analyticsEvent);

        logger.LogInformation(
            "Analytics {AnalyticsEventType};{AnalyticsPayload}",
            analyticsEvent.GetType().Name,
            JsonSerializer.Serialize(analyticsEvent, analyticsEvent.GetType()));

        return Task.CompletedTask;
    }
}

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Telemetry;

// Kirim event analitik sebagai span OpenTelemetry ke Application Insights agar bisa dianalisis lewat KQL.
// Jika pengiriman telemetry gagal, proses bisnis tetap berjalan.
public sealed class AppInsightsAnalyticsSink(
    ActivitySource activitySource,
    ILogger<AppInsightsAnalyticsSink> logger) : IAnalyticsSink
{
    public Task EmitAsync<TEvent>(TEvent analyticsEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(analyticsEvent);

        try
        {
            var eventType = analyticsEvent.GetType().Name;
            using var activity = activitySource.StartActivity($"Analytics.{eventType}", ActivityKind.Internal);
            activity?.SetTag("analytics.event_type", eventType);
            activity?.SetTag("analytics.payload", JsonSerializer.Serialize(analyticsEvent, analyticsEvent.GetType()));

            using (logger.BeginScope(new Dictionary<string, object> { ["analyticsEventType"] = eventType }))
            {
                logger.LogInformation("Analytics {AnalyticsEventType}", eventType);
            }
        }
#pragma warning disable CA1031, S2221, RCS1075
        catch (Exception)
        {
            // Tidak boleh menyentuh proses bisnis.
        }
#pragma warning restore CA1031, S2221, RCS1075

        return Task.CompletedTask;
    }
}

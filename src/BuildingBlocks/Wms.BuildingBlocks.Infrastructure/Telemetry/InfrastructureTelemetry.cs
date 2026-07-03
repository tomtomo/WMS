using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.Telemetry;

// ActivitySource, OTLP exporter saat OTEL_EXPORTER_OTLP_ENDPOINT diset, HttpClient dan Runtime, OTel logging
public static class InfrastructureTelemetry
{
    public static IServiceCollection AddInfrastructureTelemetry(this IServiceCollection services, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Idempoten: host memanggil via AddServiceDefaults dan AddBuildingBlocksInfrastructure
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TelemetryRegistrationMarker)))
        {
            return services;
        }

        services.AddSingleton(new TelemetryRegistrationMarker(serviceName));

        var otlpEnabled = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        services.AddSingleton(_ => new ActivitySource(serviceName));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(serviceName).AddHttpClientInstrumentation();
                if (otlpEnabled)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddHttpClientInstrumentation().AddRuntimeInstrumentation();
                if (otlpEnabled)
                {
                    metrics.AddOtlpExporter();
                }
            });

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            if (otlpEnabled)
            {
                options.AddOtlpExporter();
            }
        }));

        services.AddSingleton<ITelemetrySink, TelemetrySink>();

        return services;
    }
}

// Penanda "telemetry sudah ter-wire" di container, membawa nama service (registrasi pertama).
public sealed class TelemetryRegistrationMarker(string serviceName)
{
    public string ServiceName { get; } = serviceName;
}

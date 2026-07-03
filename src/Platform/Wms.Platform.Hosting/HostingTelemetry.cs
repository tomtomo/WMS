using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wms.BuildingBlocks.Infrastructure.Telemetry;

namespace Microsoft.Extensions.Hosting;

// Wiring inti OTel (ActivitySource, HttpClient+Runtime, gate OTLP, OTel logging).
public static class HostingTelemetry
{
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var serviceName = builder.Environment.ApplicationName is { Length: > 0 } name ? name : "wms-host";
        builder.Services.AddInfrastructureTelemetry(serviceName);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

        return builder;
    }
}

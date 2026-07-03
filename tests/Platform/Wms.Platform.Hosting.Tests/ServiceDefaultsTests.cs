using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wms.BuildingBlocks.Infrastructure.Telemetry;
using Xunit;

namespace Wms.Platform.Hosting.Tests;

// AddServiceDefaults pada host generik: pipeline OTel hidup, gate OTLP mengikuti env var.
public sealed class ServiceDefaultsTests
{
    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    [Fact]
    public void Add_service_defaults_resolves_tracer_and_meter_provider()
    {
        using var host = BuildHostWithServiceDefaults();

        host.Services.GetRequiredService<TracerProvider>().Should().NotBeNull();
        host.Services.GetRequiredService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void Otlp_exporter_is_not_registered_when_endpoint_env_is_absent()
    {
        var original = Environment.GetEnvironmentVariable(OtlpEndpointVariable);
        try
        {
            Environment.SetEnvironmentVariable(OtlpEndpointVariable, null);
            var builder = Host.CreateApplicationBuilder();
            builder.AddServiceDefaults();

            HasOtlpExporterWiring(builder.Services).Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtlpEndpointVariable, original);
        }
    }

    [Fact]
    public void Otlp_exporter_is_registered_when_endpoint_env_is_set()
    {
        var original = Environment.GetEnvironmentVariable(OtlpEndpointVariable);
        try
        {
            Environment.SetEnvironmentVariable(OtlpEndpointVariable, "http://localhost:4317");
            var builder = Host.CreateApplicationBuilder();
            builder.AddServiceDefaults();

            HasOtlpExporterWiring(builder.Services).Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(OtlpEndpointVariable, original);
        }
    }

    [Fact]
    public void Logging_includes_scopes_and_formatted_message_for_correlation()
    {
        using var host = BuildHostWithServiceDefaults();

        var options = host.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryLoggerOptions>>().CurrentValue;

        options.IncludeScopes.Should().BeTrue();
        options.IncludeFormattedMessage.Should().BeTrue();
    }

    [Fact]
    public void Http_client_factory_resolves_with_defaults_applied()
    {
        using var host = BuildHostWithServiceDefaults();

        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient("smoke");

        client.Should().NotBeNull();
    }

    [Fact]
    public void Add_service_defaults_is_idempotent_with_infrastructure_registration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();

        builder.Services.AddInfrastructureTelemetry("dup-service");
        using var host = builder.Build();

        host.Services.GetRequiredService<TracerProvider>().Should().NotBeNull();
        host.Services.GetServices<TelemetryRegistrationMarker>().Should().HaveCount(1);
    }

    private static IHost BuildHostWithServiceDefaults()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();
        return builder.Build();
    }

    private static bool HasOtlpExporterWiring(IServiceCollection services) =>
        services.Any(descriptor => descriptor.ServiceType == typeof(IOptionsFactory<OtlpExporterOptions>));
}

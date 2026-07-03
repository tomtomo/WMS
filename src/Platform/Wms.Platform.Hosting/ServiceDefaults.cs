namespace Microsoft.Extensions.Hosting;

// Satu panggilan per host: OTel + health + resilience + service discovery — cloud-agnostic, dipakai host Local/Azure/GCP.
public static class ServiceDefaults
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.AddDefaultHttpClientDefaults();

        return builder;
    }
}

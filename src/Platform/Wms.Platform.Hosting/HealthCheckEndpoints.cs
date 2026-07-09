using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

// Endpoint health check untuk platform runtime seperti ACA, Cloud Run, dan Aspire.
public static class HealthCheckEndpoints
{
    private const string LivenessTag = "self";

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LivenessTag]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Health check harus bisa diakses tanpa token, karena dipakai host, gateway, dan orchestrator untuk mengecek status aplikasi.
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LivenessTag),
        }).AllowAnonymous();

        return app;
    }
}

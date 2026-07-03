using Asp.Versioning;

namespace Microsoft.Extensions.DependencyInjection;

// REST versioning URL segment /v{n}/: default v1, report versi; paralel gRPC package .v1 & event vN.
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningDefaults(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}

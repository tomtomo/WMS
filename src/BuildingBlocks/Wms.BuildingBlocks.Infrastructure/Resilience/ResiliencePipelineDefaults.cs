using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Wms.BuildingBlocks.Infrastructure.Resilience;

// Polly v8 standard handler: urutan Timeout, Retry, lalu Circuit-Breaker dijamin library. Timeout HTTP sekitar 5 detik dan gRPC sekitar 30 detik.
public static class ResiliencePipelineDefaults
{
    public static readonly TimeSpan HttpTotalTimeout = TimeSpan.FromSeconds(5);

    public static readonly TimeSpan HttpAttemptTimeout = TimeSpan.FromSeconds(2);

    public static readonly TimeSpan GrpcTotalTimeout = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan GrpcAttemptTimeout = TimeSpan.FromSeconds(10);

    public static void ConfigureHttp(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TotalRequestTimeout.Timeout = HttpTotalTimeout;
        options.AttemptTimeout.Timeout = HttpAttemptTimeout;
    }

    public static void ConfigureGrpc(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TotalRequestTimeout.Timeout = GrpcTotalTimeout;
        options.AttemptTimeout.Timeout = GrpcAttemptTimeout;
    }

    // Dipakai host saat mendaftarkan HttpClient / gRPC client s2s.
    public static IHttpClientBuilder AddHttpResilience(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddStandardResilienceHandler().Configure(ConfigureHttp);
        return builder;
    }

    public static IHttpClientBuilder AddGrpcResilience(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddStandardResilienceHandler().Configure(ConfigureGrpc);
        return builder;
    }
}

using Grpc.Core;
using Microsoft.Extensions.Http.Resilience;
using Wms.BuildingBlocks.Infrastructure.Resilience;

namespace Microsoft.Extensions.DependencyInjection;

// Jalur resmi untuk gRPC internal, sudah termasuk profil resilience gRPC.
public static class InternalGrpcClient
{
    public static IHttpClientBuilder AddInternalGrpcClient<TClient>(this IServiceCollection services, Uri address)
        where TClient : ClientBase
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(address);

        // Ganti resilience HTTP default dengan profil gRPC agar timeoutnya tidak saling tumpuk.
#pragma warning disable EXTEXP0001
        return services
            .AddGrpcClient<TClient>(options => options.Address = address)
            .RemoveAllResilienceHandlers()
            .AddGrpcResilience();
#pragma warning restore EXTEXP0001
    }
}

using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Security;

// Local: gRPC internal unauth di Local. Cloud: Managed Identity (Azure) / Workload Identity (GCP).
public sealed class TrustStubServiceTokenProvider : IServiceTokenProvider
{
    public Task<string> GetTokenAsync(string audience, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        return Task.FromResult($"local-trust-stub.{audience}");
    }
}

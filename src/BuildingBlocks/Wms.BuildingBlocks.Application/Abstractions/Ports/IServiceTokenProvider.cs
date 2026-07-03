namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Token identitas service-to-service(trust-stub Local, Managed Identity Azure, Workload Identity GCP).
public interface IServiceTokenProvider
{
    Task<string> GetTokenAsync(string audience, CancellationToken cancellationToken = default);
}

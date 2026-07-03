namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Ambil secret dari store: env-var Local, Key Vault Azure, Secret Manager GCP. Tidak ada secret di source.
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}

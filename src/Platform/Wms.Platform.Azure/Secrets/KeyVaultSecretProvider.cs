using Azure;
using Azure.Security.KeyVault.Secrets;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Secrets;

// Secret Azure diambil dari Key Vault dan wajib ada saat startup.
// Verifikasi JWT tetap memakai public key secara lokal agar setiap request tidak perlu mengakses Key Vault.
public sealed class KeyVaultSecretProvider(SecretClient client) : ISecretProvider
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        KeyVaultSecret secret;
        try
        {
            secret = await client.GetSecretAsync(name, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException notFound) when (notFound.Status == 404)
        {
            throw new InvalidOperationException($"Secret '{name}' tidak ditemukan di Key Vault.", notFound);
        }

        return string.IsNullOrWhiteSpace(secret.Value)
            ? throw new InvalidOperationException($"Secret '{name}' kosong di Key Vault.")
            : secret.Value;
    }
}

using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Secrets;

// Secret dari konfigurasi "Secrets:{name}" (= env var Secrets__{name}, diinject AppHost).
public sealed class EnvSecretProvider(
    IConfiguration configuration,
    IOptions<EnvSecretOptions> options,
    ILogger<EnvSecretProvider> logger)
    : ISecretProvider
{
    private readonly Lazy<string> _devSigningKeyPem = new(
        CreateDevSigningKeyPem,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var value = configuration[$"Secrets:{name}"];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<string?>(value);
        }

        if (string.Equals(name, options.Value.SigningKeySecretName, StringComparison.Ordinal))
        {
            // Jika signing key belum dikonfigurasi, gunakan key sementara untuk development dan tampilkan warning.
            // Token yang dibuat tidak akan tetap valid setelah restart atau saat dipakai di instance lain.
            logger.LogWarning(
                "Secret '{SecretName}' tidak diset — memakai signing key RSA EPHEMERAL per-proses (dev-only). "
                + "Token tidak akan valid lintas restart/instance; set env var 'Secrets__' + nama itu untuk key stabil.",
                name);
            return Task.FromResult<string?>(_devSigningKeyPem.Value);
        }

        throw new InvalidOperationException(
            $"Secret '{name}' tidak ditemukan. Set env var 'Secrets__{name}' (AppHost) atau konfigurasi 'Secrets:{name}'.");
    }

    private static string CreateDevSigningKeyPem()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportPkcs8PrivateKeyPem();
    }
}

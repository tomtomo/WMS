using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Secrets;

// Secret dari konfigurasi "Secrets:{name}" (= env var Secrets__{name}, diinject AppHost).
public sealed class EnvSecretProvider(IConfiguration configuration, IOptions<EnvSecretOptions> options)
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

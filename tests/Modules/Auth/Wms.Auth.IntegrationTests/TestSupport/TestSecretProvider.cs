using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Menyediakan secret untuk kebutuhan test
internal sealed class TestSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Equals(name, TestJwtKeys.SigningKeySecretName, StringComparison.Ordinal)
            ? TestJwtKeys.PrivateKeyPem
            : null);
}

// ISecretProvider yang selalu mengembalikan null.
internal sealed class NullSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

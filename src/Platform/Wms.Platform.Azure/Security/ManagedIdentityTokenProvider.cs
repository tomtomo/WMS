using System.Collections.Concurrent;
using Azure.Core;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Security;

// Token service to service dari Managed Identity dan hanya berlaku untuk audience yang dituju.
// Penerima memvalidasi algoritma dan audience tanpa perlu memanggil auth service.
public sealed class ManagedIdentityTokenProvider(TokenCredential credential, TimeProvider timeProvider)
    : IServiceTokenProvider
{
    private const string DefaultScopeSuffix = "/.default";

    // Perbarui token sebelum masa berlakunya habis agar request tidak memakai token yang hampir kedaluwarsa.
    private static readonly TimeSpan _refreshMargin = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, AccessToken> _tokens = new(StringComparer.Ordinal);

    public async Task<string> GetTokenAsync(string audience, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var scope = ToScope(audience);
        if (_tokens.TryGetValue(scope, out var cached) && IsUsable(cached))
        {
            return cached.Token;
        }

        var token = await credential
            .GetTokenAsync(new TokenRequestContext([scope]), cancellationToken)
            .ConfigureAwait(false);
        _tokens[scope] = token;
        return token.Token;
    }

    private static string ToScope(string audience) =>
        audience.EndsWith(DefaultScopeSuffix, StringComparison.Ordinal)
            ? audience
            : audience.TrimEnd('/') + DefaultScopeSuffix;

    private bool IsUsable(AccessToken token) => timeProvider.GetUtcNow() + _refreshMargin < token.ExpiresOn;
}

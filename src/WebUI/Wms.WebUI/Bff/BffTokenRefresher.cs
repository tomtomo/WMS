using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wms.WebUI.Bff;

// Refresh access token sebelum kedaluwarsa.
// Lock per sesi mencegah refresh token single-use dipakai bersamaan. client tanpa bearer handler mencegah refresh berulang.
internal sealed class BffTokenRefresher(IHttpClientFactory httpClientFactory, ITokenStore tokenStore) : IDisposable
{
    // Sisakan waktu aman agar token tidak kedaluwarsa saat request masih berjalan.
    private static readonly TimeSpan _expirySkew = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshGates = new(StringComparer.Ordinal);

    // Pakai access token yang masih aman, lalu refresh saat mendekati kedaluwarsa. Null berarti sesi tidak punya token.
    public async Task<string?> GetValidAccessTokenAsync(string sessionId, CancellationToken cancellationToken)
    {
        var current = tokenStore.Get(sessionId);
        if (current is null)
        {
            return null;
        }

        if (!IsExpiring(current))
        {
            return current.AccessToken;
        }

        var gate = _refreshGates.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Cek ulang setelah mendapat lock karena request lain mungkin sudah memperbarui token lebih dulu.
            current = tokenStore.Get(sessionId);
            if (current is null)
            {
                return null;
            }

            if (!IsExpiring(current))
            {
                return current.AccessToken;
            }

            var refreshed = await TryRefreshAsync(current.RefreshToken, cancellationToken);
            if (refreshed is null)
            {
                // Jika refresh gagal, pakai token lama dan biarkan respons 401 meminta pengguna login kembali.
                return current.AccessToken;
            }

            tokenStore.Set(sessionId, refreshed);
            return refreshed.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    // Hapus gate sesi saat logout agar koleksi tidak terus bertambah.
    public void Forget(string sessionId)
    {
        if (_refreshGates.TryRemove(sessionId, out var gate))
        {
            gate.Dispose();
        }
    }

    // Bersihkan semua gate yang masih tersimpan saat aplikasi berhenti.
    public void Dispose()
    {
        foreach (var gate in _refreshGates.Values)
        {
            gate.Dispose();
        }

        _refreshGates.Clear();
    }

    private static bool IsExpiring(TokenSet tokens) => tokens.ExpiresAt <= DateTimeOffset.UtcNow + _expirySkew;

    private async Task<TokenSet?> TryRefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(BffExtensions.AuthRefreshClientName);
            using var response = await client.PostAsJsonAsync(
                "/auth/v1/refresh", new { refreshToken }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<GatewayTokenResponse>(cancellationToken);
            return token is null ? null : new TokenSet(token.AccessToken, token.RefreshToken, token.ExpiresAt);
        }
        catch (HttpRequestException)
        {
            // Gateway atau layanan Auth tidak dapat dihubungi, jadi gunakan token lama.
            return null;
        }
        catch (JsonException)
        {
            // Respons dari layanan tidak sesuai dengan format yang diharapkan.
            return null;
        }
    }
}

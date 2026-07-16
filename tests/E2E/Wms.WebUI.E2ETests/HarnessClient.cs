using System.Net;
using System.Net.Http.Json;

namespace Wms.WebUI.E2ETests;

// Harness REST sesuai opsi 1 pada charter §3.2: login langsung ke Auth untuk mendapat JWT internal,
// lalu panggil endpoint berproteksi dengan Bearer token. Ini dipakai untuk probe 403 dan Idempotency-Key
// yang tidak bisa dijalankan lewat Playwright karena browser hanya menyimpan cookie sesi.
public sealed class HarnessClient : IDisposable
{
    private readonly HttpClient _client;

    public HarnessClient(string gatewayUrl, bool ignoreTls)
    {
        var handler = new HttpClientHandler();
        if (ignoreTls)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        _client = new HttpClient(handler) { BaseAddress = new Uri(gatewayUrl) };
    }

    public async Task<string> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync("/auth/v1/login", new { username, password }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        return token!.AccessToken;
    }

    public async Task<HttpStatusCode> PostStatusAsync(
        string token,
        string path,
        object body,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await _client.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }

    // GET dengan Bearer token untuk mencari entitas milik test ini dan mengecek hasil akhirnya.
    public async Task<T?> GetJsonAsync<T>(string token, string path, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    // POST dengan Bearer token lalu baca responsnya saat perlu mengambil ID atau memeriksa replay.
    public async Task<T?> PostReadAsync<T>(
        string token,
        string path,
        object body,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    // POST dengan Bearer token dan pastikan berhasil. Body boleh kosong.
    public async Task PostOkAsync(string token, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _client.Dispose();

    private sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
}

using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Wms.WebUI.Bff;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test refresh token BFF tanpa menunggu token benar-benar kedaluwarsa.
// Simpan token hasil rotasi saat refresh berhasil, atau pakai token lama jika refresh gagal.
public sealed class BffTokenRefresherTests
{
    private const string Session = "sess-1";

    [Fact]
    public async Task Fresh_token_is_returned_without_calling_refresh()
    {
        var handler = new StubRefreshHandler();
        var store = new InMemoryTokenStore();
        store.Set(Session, new TokenSet("access-fresh", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(30)));
        var refresher = new BffTokenRefresher(new StubFactory(handler), store);

        var token = await refresher.GetValidAccessTokenAsync(Session, CancellationToken.None);

        token.Should().Be("access-fresh");
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Expiring_token_is_refreshed_and_rotation_persisted()
    {
        var handler = new StubRefreshHandler
        {
            Body = TokenJson("access-new", "refresh-2", DateTimeOffset.UtcNow.AddMinutes(30)),
        };
        var store = new InMemoryTokenStore();
        store.Set(Session, new TokenSet("access-old", "refresh-1", DateTimeOffset.UtcNow.AddSeconds(-5)));
        var refresher = new BffTokenRefresher(new StubFactory(handler), store);

        var token = await refresher.GetValidAccessTokenAsync(Session, CancellationToken.None);

        token.Should().Be("access-new");
        handler.Calls.Should().Be(1);
        handler.LastBody.Should().Contain("refresh-1");             // refresh memakai token lama
        var stored = store.Get(Session);
        stored!.AccessToken.Should().Be("access-new");
        stored.RefreshToken.Should().Be("refresh-2");               // simpan refresh token hasil rotasi
    }

    [Fact]
    public async Task Refresh_failure_falls_back_to_old_access_token()
    {
        var handler = new StubRefreshHandler { Status = HttpStatusCode.Unauthorized };
        var store = new InMemoryTokenStore();
        store.Set(Session, new TokenSet("access-old", "refresh-1", DateTimeOffset.UtcNow.AddSeconds(-5)));
        var refresher = new BffTokenRefresher(new StubFactory(handler), store);

        var token = await refresher.GetValidAccessTokenAsync(Session, CancellationToken.None);

        token.Should().Be("access-old");                            // biarkan pengguna login ulang jika downstream membalas 401
        store.Get(Session)!.RefreshToken.Should().Be("refresh-1");  // refresh token lama tetap tersimpan
    }

    [Fact]
    public async Task Unknown_session_returns_null()
    {
        var refresher = new BffTokenRefresher(new StubFactory(new StubRefreshHandler()), new InMemoryTokenStore());

        (await refresher.GetValidAccessTokenAsync("unknown", CancellationToken.None)).Should().BeNull();
    }

    private static string TokenJson(string access, string refresh, DateTimeOffset expiresAt) =>
        JsonSerializer.Serialize(new { accessToken = access, expiresAt, refreshToken = refresh });

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler) { BaseAddress = new Uri("https://gateway.local") };
    }

    private sealed class StubRefreshHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? LastBody { get; private set; }

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public string Body { get; set; } =
            JsonSerializer.Serialize(new { accessToken = "x", expiresAt = DateTimeOffset.MaxValue, refreshToken = "y" });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            };
        }
    }
}

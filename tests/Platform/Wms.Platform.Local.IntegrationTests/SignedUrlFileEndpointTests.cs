using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Wms.Platform.Local.ObjectStore;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Memastikan endpoint file hanya menerima signed URL yang masih valid. URL yang diubah atau sudah expired harus ditolak.
public sealed class SignedUrlFileEndpointTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(_now);
    private string _rootPath = string.Empty;
    private FileSystemObjectStore _objectStore = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), $"wms-objstore-{Guid.NewGuid():N}");
        _objectStore = new FileSystemObjectStore(
            Options.Create(new FileSystemObjectStoreOptions
            {
                RootPath = _rootPath,
                BaseUrl = new Uri("http://localhost/files"),
                SigningKeyBase64 = Convert.ToBase64String(new byte[32]),
            }),
            _clock);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_objectStore);
        _app = builder.Build();

        _app.MapGet("/files/{**path}", async (HttpContext httpContext, FileSystemObjectStore objectStore) =>
        {
            var requestUrl = new Uri(
                $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}");
            if (!objectStore.TryValidateReadUrl(requestUrl, out var objectPath) || objectPath is null)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var stream = await objectStore.GetAsync(objectPath, httpContext.RequestAborted);
            return Results.Stream(stream, "application/octet-stream");
        });
        await _app.StartAsync();
        _client = _app.GetTestClient();

        var content = new MemoryStream(Encoding.UTF8.GetBytes("hello-signed"));
        await using (content.ConfigureAwait(false))
        {
            await _objectStore.PutAsync("attachments/hello.txt", content, "text/plain");
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task Valid_signed_url_streams_file_bytes()
    {
        var signedUrl = _objectStore.CreateReadUrl("attachments/hello.txt", TimeSpan.FromMinutes(5));

        var response = await _client.GetAsync(signedUrl.PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("hello-signed");
    }

    [Fact]
    public async Task Tampered_signature_is_forbidden()
    {
        var signedUrl = _objectStore.CreateReadUrl("attachments/hello.txt", TimeSpan.FromMinutes(5));
        var tampered = signedUrl.PathAndQuery.Replace("&sig=", "&sig=Z", StringComparison.Ordinal);

        var response = await _client.GetAsync(tampered);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Expired_signed_url_is_forbidden()
    {
        var signedUrl = _objectStore.CreateReadUrl("attachments/hello.txt", TimeSpan.FromMinutes(5));
        _clock.Advance(TimeSpan.FromMinutes(10));

        var response = await _client.GetAsync(signedUrl.PathAndQuery);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

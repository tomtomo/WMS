using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.ObjectStore;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

public sealed class FileSystemObjectStoreTests : IDisposable
{
    private static readonly DateTimeOffset _epoch = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "wms-objectstore-" + Guid.NewGuid().ToString("N"));
    private readonly MutableTimeProvider _clock = new(_epoch);
    private readonly FileSystemObjectStore _store;

    public FileSystemObjectStoreTests()
    {
        var options = Options.Create(new FileSystemObjectStoreOptions
        {
            RootPath = _rootPath,
            BaseUrl = new Uri("http://localhost:5099/objects"),
        });
        _store = new FileSystemObjectStore(options, _clock);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task Put_then_get_round_trips_content()
    {
        var payload = Encoding.UTF8.GetBytes("isi attachment GR");
        using var upload = new MemoryStream(payload);

        await _store.PutAsync("gr-1/att-1/receipt.pdf", upload, "application/pdf");

        var download = await _store.GetAsync("gr-1/att-1/receipt.pdf");
        await using (download.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await download.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(payload);
        }
    }

    [Fact]
    public async Task Get_missing_object_throws_file_not_found()
    {
        var act = () => _store.GetAsync("gr-x/att-x/missing.pdf");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("gr-1/../../escape.txt")]
    [InlineData("gr-1/./att/file.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/windows/system32/config")]
    [InlineData(@"gr-1\att-1\file.txt")]
    [InlineData("gr-1//file.txt")]
    public async Task Path_traversal_and_rooted_paths_are_rejected(string maliciousPath)
    {
        using var upload = new MemoryStream([1, 2, 3]);

        var act = () => _store.PutAsync(maliciousPath, upload, "application/octet-stream");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Read_url_is_valid_before_ttl_and_scoped_to_path()
    {
        var url = _store.CreateReadUrl("gr-1/att-1/receipt.pdf", TimeSpan.FromMinutes(10));

        _store.TryValidateReadUrl(url, out var path).Should().BeTrue();
        path.Should().Be("gr-1/att-1/receipt.pdf");
    }

    [Fact]
    public void Read_url_survives_reserved_characters_in_file_name()
    {
        // Nama file dunia-nyata: spasi, kurung, plus — encode-then-sign wajib konsisten dua sisi.
        var url = _store.CreateReadUrl("gr-1/att-1/Invoice 2024 (final)+rev.pdf", TimeSpan.FromMinutes(10));

        _store.TryValidateReadUrl(url, out var path).Should().BeTrue();
        path.Should().Be("gr-1/att-1/Invoice 2024 (final)+rev.pdf");
    }

    [Fact]
    public void Read_url_expires_after_ttl()
    {
        var url = _store.CreateReadUrl("gr-1/att-1/receipt.pdf", TimeSpan.FromMinutes(10));

        _clock.Advance(TimeSpan.FromMinutes(11));

        _store.TryValidateReadUrl(url, out _).Should().BeFalse();
    }

    [Fact]
    public void Tampered_signature_is_rejected()
    {
        var url = _store.CreateReadUrl("gr-1/att-1/receipt.pdf", TimeSpan.FromMinutes(10));
        var tampered = new Uri(url.AbsoluteUri.Replace("sig=", "sig=x", StringComparison.Ordinal));

        _store.TryValidateReadUrl(tampered, out _).Should().BeFalse();
    }

    [Fact]
    public void Url_signed_for_one_path_does_not_authorize_another()
    {
        var url = _store.CreateReadUrl("gr-1/att-1/receipt.pdf", TimeSpan.FromMinutes(10));
        var swappedPath = new Uri(url.AbsoluteUri.Replace("att-1", "att-2", StringComparison.Ordinal));

        _store.TryValidateReadUrl(swappedPath, out _).Should().BeFalse();
    }
}

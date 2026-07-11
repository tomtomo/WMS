using System.Net;
using System.Text;
using AwesomeAssertions;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Valet Key membuat URL sementara untuk satu blob tanpa account key, dan file diakses langsung tanpa melewati service.
// Test dijalankan memakai storage account nyata di resource group khusus test.
[Collection(BlobCollection.Name)]
public sealed class ObjectStoreSasParityTests(BlobFixture fixture)
{
    private static readonly byte[] _content = Encoding.UTF8.GetBytes("nota-penerimaan-barang");

    // Marker ini memastikan SAS ditandatangani dengan user delegation, bukan account key.
    private static readonly string[] _userDelegationMarkers = ["skoid=", "sktid=", "skt=", "ske=", "sks=", "skv="];

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Stored_object_round_trips_through_the_container()
    {
        Skip.IfNot(fixture.IsAvailable, "Storage live tak dikonfigurasi (WMS_PARITY_BLOB_*).");
        var store = fixture.CreateStore();
        var path = NewAttachmentPath();

        await store.PutAsync(path, new MemoryStream(_content), "application/pdf");

        var stream = await store.GetAsync(path);
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(_content);
        }
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Missing_object_reads_as_a_not_found_failure()
    {
        Skip.IfNot(fixture.IsAvailable, "Storage live tak dikonfigurasi (WMS_PARITY_BLOB_*).");

        var get = () => fixture.CreateStore().GetAsync(NewAttachmentPath());

        await get.Should().ThrowAsync<FileNotFoundException>();
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Read_url_is_a_user_delegation_sas_that_downloads_the_blob_directly()
    {
        Skip.IfNot(fixture.IsAvailable, "Storage live tak dikonfigurasi (WMS_PARITY_BLOB_*).");
        var store = fixture.CreateStore();
        var path = NewAttachmentPath();
        await store.PutAsync(path, new MemoryStream(_content), "application/pdf");

        var readUrl = store.CreateReadUrl(path, TimeSpan.FromMinutes(5));

        // Pastikan URL ditandatangani dengan user delegation key, bukan account key.
        _userDelegationMarkers.Should().AllSatisfy(marker => readUrl.Query.Should().Contain(marker));

        // Pastikan file didownload langsung dari Storage tanpa melewati service.
        readUrl.Host.Should().Be(fixture.StoreOptions.AccountUrl!.Host);

        using var http = new HttpClient();
        var response = await http.GetAsync(readUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(_content);
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Read_url_of_one_blob_does_not_open_another_blob()
    {
        Skip.IfNot(fixture.IsAvailable, "Storage live tak dikonfigurasi (WMS_PARITY_BLOB_*).");
        var store = fixture.CreateStore();
        var granted = NewAttachmentPath();
        var forbidden = NewAttachmentPath();
        await store.PutAsync(granted, new MemoryStream(_content), "application/pdf");
        await store.PutAsync(forbidden, new MemoryStream(_content), "application/pdf");

        var readUrl = store.CreateReadUrl(granted, TimeSpan.FromMinutes(5));
        var swapped = new UriBuilder(readUrl) { Path = readUrl.AbsolutePath.Replace(granted, forbidden, StringComparison.Ordinal) }.Uri;

        using var http = new HttpClient();
        var response = await http.GetAsync(swapped);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Read_url_stops_working_once_its_ttl_expires()
    {
        Skip.IfNot(fixture.IsAvailable, "Storage live tak dikonfigurasi (WMS_PARITY_BLOB_*).");
        var store = fixture.CreateStore();
        var path = NewAttachmentPath();
        await store.PutAsync(path, new MemoryStream(_content), "application/pdf");

        var readUrl = store.CreateReadUrl(path, TimeSpan.FromSeconds(5));
        await Task.Delay(TimeSpan.FromSeconds(8));

        using var http = new HttpClient();
        var response = await http.GetAsync(readUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // blobPath berbentuk {grId}/{attachmentId}/{fileName}, identik dengan Local.
    private static string NewAttachmentPath() => $"gr-{Guid.NewGuid():N}/att-{Guid.NewGuid():N}/nota.pdf";
}

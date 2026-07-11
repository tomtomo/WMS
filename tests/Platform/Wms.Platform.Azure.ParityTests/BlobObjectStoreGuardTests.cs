using AwesomeAssertions;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Wms.Platform.Azure.ObjectStore;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Validasi path dan masa berlaku dilakukan sebelum mengakses storage account, jadi bisa ditest tanpa koneksi jaringan.
public sealed class BlobObjectStoreGuardTests
{
    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("gr/../../secret.pdf")]
    [InlineData("gr\\att\\file.pdf")]
    [InlineData("/gr/att/file.pdf")]
    [InlineData("C:/gr/att/file.pdf")]
    [InlineData("gr//file.pdf")]
    [InlineData(" ")]
    public void Read_url_rejects_a_path_that_escapes_its_attachment_prefix(string blobPath)
    {
        var createReadUrl = () => NewStore().CreateReadUrl(blobPath, TimeSpan.FromMinutes(5));

        createReadUrl.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Read_url_rejects_a_non_positive_ttl()
    {
        var createReadUrl = () => NewStore().CreateReadUrl("gr-1/att-1/nota.pdf", TimeSpan.Zero);

        createReadUrl.Should().Throw<ArgumentOutOfRangeException>();
    }

    // Kredensial tidak pernah dipakai karena validasi menolak lebih dulu, sebelum ada penukaran user delegation key.
    private static BlobObjectStore NewStore() =>
        new(
            new BlobServiceClient(new Uri("https://wms-offline.blob.core.windows.net"), new DefaultAzureCredential()),
            Options.Create(new BlobObjectStoreOptions
            {
                AccountUrl = new Uri("https://wms-offline.blob.core.windows.net"),
                ContainerName = "gr-attachments",
            }),
            TimeProvider.System);
}

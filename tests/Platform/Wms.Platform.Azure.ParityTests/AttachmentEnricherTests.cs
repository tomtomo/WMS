using AwesomeAssertions;
using Wms.Platform.Azure.Enrichment;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan checksum dan pengecekan enrichment dapat ditest tanpa binding atau koneksi storage.
public sealed class AttachmentEnricherTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private static readonly DateTimeOffset _enrichedAt = new(2026, 7, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compute_sha256_matches_known_vector()
    {
        AttachmentEnricher.ComputeSha256Hex("abc"u8).Should().Be(AbcSha256);
    }

    [Fact]
    public void Already_enriched_when_checksum_metadata_present()
    {
        var metadata = new Dictionary<string, string> { ["sha256"] = "deadbeef" };

        AttachmentEnricher.IsAlreadyEnriched(metadata).Should().BeTrue();
    }

    [Fact]
    public void Not_enriched_when_checksum_metadata_absent()
    {
        AttachmentEnricher.IsAlreadyEnriched(new Dictionary<string, string>()).Should().BeFalse();
    }

    [Fact]
    public void Build_metadata_adds_checksum_and_timestamp_preserving_existing()
    {
        var existing = new Dictionary<string, string> { ["origin"] = "upload" };

        var result = AttachmentEnricher.BuildEnrichmentMetadata("abc"u8, existing, _enrichedAt);

        result.Should().NotBeNull();
        result!["origin"].Should().Be("upload");
        result["sha256"].Should().Be(AbcSha256);
        result["enrichedAt"].Should().Be("2026-07-12T08:00:00.0000000+00:00");
    }

    [Fact]
    public void Build_metadata_returns_null_when_already_enriched()
    {
        var existing = new Dictionary<string, string> { ["sha256"] = "existing" };

        var result = AttachmentEnricher.BuildEnrichmentMetadata("abc"u8, existing, _enrichedAt);

        result.Should().BeNull();
    }

    [Fact]
    public void Extract_blob_name_reads_nested_path_after_blobs_marker()
    {
        const string Subject = "/blobServices/default/containers/gr-attachments/blobs/gr-1/att-1/nota.pdf";

        AttachmentEnricher.TryExtractBlobName(Subject, "gr-attachments").Should().Be("gr-1/att-1/nota.pdf");
    }

    [Theory]
    [InlineData("/blobServices/default/containers/file-drop/blobs/x.csv")]
    [InlineData("/blobServices/default/containers/gr-attachments/blobs/")]
    [InlineData("")]
    [InlineData(null)]
    public void Extract_blob_name_returns_null_for_other_container_or_empty(string? subject)
    {
        AttachmentEnricher.TryExtractBlobName(subject, "gr-attachments").Should().BeNull();
    }
}

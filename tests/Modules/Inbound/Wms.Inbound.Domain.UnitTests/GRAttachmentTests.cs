using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// GRAttachment: metadata dokumen pendukung GR
public sealed class GRAttachmentTests
{
    private const long FiftyMegabytes = 50L * 1024 * 1024;

    private static readonly DateTimeOffset _uploadedAt = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_succeeds_and_starts_active()
    {
        var result = Create();

        result.IsSuccess.Should().BeTrue();
        var attachment = result.Value;
        attachment.FileName.Should().Be("surat-jalan.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.SizeBytes.Should().Be(1024);
        attachment.UploadedAt.Should().Be(_uploadedAt);
        attachment.IsActive.Should().BeTrue();
        attachment.Should().BeAssignableTo<IAuditable>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_file_name_as_invalid(string fileName)
    {
        var result = Create(fileName: fileName);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("gr_attachment.file_name_required");
    }

    [Fact]
    public void Create_accepts_a_file_name_of_exactly_256_chars()
    {
        Create(fileName: new string('a', 252) + ".pdf").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_a_file_name_longer_than_256_chars_as_invalid()
    {
        var result = Create(fileName: new string('a', 253) + ".pdf");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("gr_attachment.file_name_too_long");
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Create_accepts_every_whitelisted_content_type(string contentType)
    {
        Create(contentType: contentType).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_matches_the_content_type_case_insensitively()
    {
        Create(contentType: "IMAGE/JPEG").IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("text/html")]
    [InlineData("")]
    public void Create_rejects_a_content_type_outside_the_whitelist_as_invalid(string contentType)
    {
        var result = Create(contentType: contentType);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("gr_attachment.content_type_forbidden");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(FiftyMegabytes + 1)]
    public void Create_rejects_a_size_outside_the_range_as_invalid(long sizeBytes)
    {
        var result = Create(sizeBytes: sizeBytes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("gr_attachment.size_out_of_range");
    }

    [Fact]
    public void Create_accepts_a_size_of_exactly_50_megabytes()
    {
        Create(sizeBytes: FiftyMegabytes).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Soft_delete_deactivates_and_stays_idempotent()
    {
        var attachment = Create().Value;

        attachment.SoftDelete();
        attachment.SoftDelete();

        attachment.IsActive.Should().BeFalse();
    }

    private static Result<GRAttachment> Create(
        string fileName = "surat-jalan.pdf",
        string contentType = "application/pdf",
        long sizeBytes = 1024)
        => GRAttachment.Create(
            GRAttachmentId.Create(Guid.NewGuid()).Value,
            GoodsReceiptMother.NewId(),
            fileName,
            contentType,
            sizeBytes,
            ContentRef.Create("gr-1/att-1/surat-jalan.pdf").Value,
            _uploadedAt);
}

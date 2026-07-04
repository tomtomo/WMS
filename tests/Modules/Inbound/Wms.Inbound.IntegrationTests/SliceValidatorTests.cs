using AwesomeAssertions;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;
using Wms.Inbound.Application.Features.UploadGRAttachment;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Validator — bentuk request ditolak sebelum handler
public sealed class SliceValidatorTests
{
    [Fact]
    public void Create_tanpa_expected_lines_tidak_valid()
    {
        var command = new CreateGoodsReceiptHeaderCommand("PO-1", Guid.NewGuid(), Guid.NewGuid(), "DOCK-1", []);

        var result = new CreateGoodsReceiptHeaderValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.ExpectedLines));
    }

    [Fact]
    public void Create_line_qty_nol_tidak_valid()
    {
        var command = new CreateGoodsReceiptHeaderCommand(
            "PO-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DOCK-1",
            [new ExpectedLineInput("SKU-A", 0m, "EA")]);

        new CreateGoodsReceiptHeaderValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("../escape.pdf")]
    [InlineData(@"folder\file.pdf")]
    public void Upload_filename_dengan_separator_path_tidak_valid(string fileName)
    {
        using var content = new MemoryStream([1]);
        var command = new UploadGRAttachmentCommand(Guid.NewGuid(), fileName, "application/pdf", 1, content);

        var result = new UploadGRAttachmentValidator().Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Upload_size_di_atas_cap_domain_tidak_valid()
    {
        using var content = new MemoryStream([1]);
        var command = new UploadGRAttachmentCommand(
            Guid.NewGuid(),
            "a.pdf",
            "application/pdf",
            (50L * 1024 * 1024) + 1,
            content);

        new UploadGRAttachmentValidator().Validate(command).IsValid.Should().BeFalse();
    }
}

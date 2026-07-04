using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Domain;

// Metadata dokumen pendukung GR — aggregate root terpisah agar upload bertahap tanpa memuat penuh GR.
public sealed class GRAttachment : AggregateRoot<GRAttachmentId>, IAuditable
{
    public const long MaxSizeBytes = 50L * 1024 * 1024;

    public const int MaxFileNameLength = 256;

    private static readonly string[] _allowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    ];

    private GRAttachment(
        GRAttachmentId id,
        GoodsReceiptId goodsReceiptId,
        string fileName,
        string contentType,
        long sizeBytes,
        ContentRef contentRef,
        DateTimeOffset uploadedAt)
        : base(id)
    {
        GoodsReceiptId = goodsReceiptId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        ContentRef = contentRef;
        UploadedAt = uploadedAt;
        IsActive = true;
    }

    public GoodsReceiptId GoodsReceiptId { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public ContentRef ContentRef { get; }

    // Kapan dokumen diupload — domain tidak membaca clock (TimeProvider).
    public DateTimeOffset UploadedAt { get; }

    public bool IsActive { get; private set; }

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<GRAttachment> Create(
        GRAttachmentId id,
        GoodsReceiptId goodsReceiptId,
        string fileName,
        string contentType,
        long sizeBytes,
        ContentRef contentRef,
        DateTimeOffset uploadedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(goodsReceiptId);
        ArgumentNullException.ThrowIfNull(contentRef);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Invalid<GRAttachment>(new Error("gr_attachment.file_name_required", "FileName wajib diisi."));
        }

        var trimmedFileName = fileName.Trim();
        if (trimmedFileName.Length > MaxFileNameLength)
        {
            return Result.Invalid<GRAttachment>(new Error("gr_attachment.file_name_too_long", "FileName maksimal 256 karakter."));
        }

        // MIME type case insensitive per RFC
        var trimmedContentType = contentType?.Trim() ?? string.Empty;
        if (!_allowedContentTypes.Contains(trimmedContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Invalid<GRAttachment>(new Error("gr_attachment.content_type_forbidden", "ContentType di luar whitelist dokumen GR."));
        }

        if (sizeBytes <= 0 || sizeBytes > MaxSizeBytes)
        {
            return Result.Invalid<GRAttachment>(new Error("gr_attachment.size_out_of_range", "SizeBytes harus di rentang 1 byte s.d. 50 MB."));
        }

        return Result.Success(new GRAttachment(id, goodsReceiptId, trimmedFileName, trimmedContentType, sizeBytes, contentRef, uploadedAt));
    }

    public void SoftDelete() => IsActive = false;
}

namespace Wms.Inbound.Application.ReadModels;

// Metadata attachment untuk listing
public sealed record GRAttachmentDto(
    Guid AttachmentId,
    Guid GoodsReceiptId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    bool IsActive);

namespace Wms.Inbound.Application.ReadModels;

public sealed record GoodsReceiptReviewDto(
    Guid GoodsReceiptId,
    string Status,
    bool HasOverDelivery,
    int UnresolvedCount,
    IReadOnlyList<ReviewLineDto> Lines,
    IReadOnlyList<DiscrepancyGroupDto> DiscrepancyGroups);

public sealed record ReviewLineDto(string Sku, string Uom, decimal ExpectedQty, decimal ActualQty, string Variance);

public sealed record DiscrepancyGroupDto(string Sku, string Type, IReadOnlyList<DiscrepancyItemDto> Items);

public sealed record DiscrepancyItemDto(Guid DiscrepancyId, decimal Qty, bool Resolved, string? Action, string? Note);

namespace Wms.MasterData.Application.ReadModels;

public sealed record ProductSnapshotDto(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays,
    bool IsActive);

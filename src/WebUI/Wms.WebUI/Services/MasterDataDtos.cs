namespace Wms.WebUI.Services;

// DTO Product, Warehouse, dan Location digabung agar pola filenya sama dengan modul lain.

// DTO produk yang mengikuti struktur ProductSnapshotDto dari backend.
public sealed record ProductDto(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays,
    bool IsActive);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays);

public sealed record UpdateProductRequest(
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays);

public sealed record WarehouseDto(Guid WarehouseId, string Name, string Address, bool IsActive);

public sealed record CreateWarehouseRequest(string Name, string Address);

public sealed record UpdateWarehouseRequest(string Name, string Address);

// Ringkasan LocationDto backend.
public sealed record LocationDto(Guid LocationId, Guid WarehouseId, string Type, string Code, bool IsActive);

public sealed record CreateLocationRequest(Guid WarehouseId, string Type, string Code);

public sealed record UpdateLocationRequest(string Type, string Code);

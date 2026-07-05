using Wms.MasterData.Domain;
using Wms.MasterData.Domain.Enums;

namespace Wms.MasterData.Domain.UnitTests.TestData;

// aggregate MasterData valid untuk behavior test.
internal static class MasterDataMother
{
    public static WarehouseId NewWarehouseId() => WarehouseId.Create(Guid.NewGuid()).Value;

    public static LocationId NewLocationId() => LocationId.Create(Guid.NewGuid()).Value;

    public static Sku SkuOf(string code = "SKU-MILK") => Sku.Create(code).Value;

    public static Warehouse AWarehouse(string name = "DC Jakarta Cakung", string address = "Jl. Raya Cakung No. 1")
        => Warehouse.Create(NewWarehouseId(), name, address).Value;

    public static Location ALocation(LocationType type = LocationType.Rack, string code = "RACK-B12-03")
        => Location.Create(NewLocationId(), NewWarehouseId(), type, code).Value;

    public static Product AProduct(
        string sku = "SKU-MILK",
        string name = "Fresh Milk 1L",
        string uom = "carton",
        bool batchTrackingRequired = true,
        bool expiryTrackingRequired = true,
        bool qcRequiredOnReceipt = false,
        int? shelfLifeDays = 30)
        => Product.Create(
            SkuOf(sku), name, uom, batchTrackingRequired, expiryTrackingRequired, qcRequiredOnReceipt, shelfLifeDays).Value;
}

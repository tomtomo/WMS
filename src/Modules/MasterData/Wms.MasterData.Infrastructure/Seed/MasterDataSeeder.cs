using Microsoft.EntityFrameworkCore;
using Wms.MasterData.Domain;
using Wms.MasterData.Domain.Enums;

namespace Wms.MasterData.Infrastructure.Seed;

// Seed referensi minimal MasterData: 1 Warehouse, 4 Location.
public static class MasterDataSeeder
{
    // Well-known IDs deterministik — idempotent + bisa dirujuk downstream/seed test.
    public static readonly Guid DefaultWarehouseId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid ReceivingLocationId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid RackLocationId = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid QuarantineLocationId = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid StagingLocationId = Guid.Parse("b0000000-0000-0000-0000-000000000004");

    public static async Task SeedAsync(MasterDataDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Idempotent
        if (await context.Set<Warehouse>().IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var warehouseId = WarehouseId.Create(DefaultWarehouseId).Value;
        context.Add(Warehouse.Create(warehouseId, "DC Jakarta Cakung", "Jl. Raya Cakung No. 1").Value);

        context.Add(Location.Create(LocationId.Create(ReceivingLocationId).Value, warehouseId, LocationType.ReceivingArea, "REC-01").Value);
        context.Add(Location.Create(LocationId.Create(RackLocationId).Value, warehouseId, LocationType.Rack, "RACK-B12-03").Value);
        context.Add(Location.Create(LocationId.Create(QuarantineLocationId).Value, warehouseId, LocationType.QuarantineArea, "QC-A").Value);
        context.Add(Location.Create(LocationId.Create(StagingLocationId).Value, warehouseId, LocationType.StagingArea, "STG-2").Value);

        context.Add(Product.Create(Sku.Create("SKU-MILK").Value, "Fresh Milk 1L", "carton", true, true, false, 30).Value);
        context.Add(Product.Create(Sku.Create("SKU-YOGURT").Value, "Yogurt 500g", "carton", true, true, true, 21).Value);
        context.Add(Product.Create(Sku.Create("SKU-RICE").Value, "Rice 5kg", "sack", false, false, false, null).Value);

        await context.SaveChangesAsync(cancellationToken);
    }
}

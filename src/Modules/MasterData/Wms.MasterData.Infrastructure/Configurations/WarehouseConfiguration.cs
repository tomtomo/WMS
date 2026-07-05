using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Configurations;

// Mapping Warehouse
public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("warehouse");

        builder.HasKey(warehouse => warehouse.Id);
        builder.Property(warehouse => warehouse.Id)
            .HasConversion(id => id.Value, value => WarehouseId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(warehouse => warehouse.Name).HasMaxLength(200);
        builder.Property(warehouse => warehouse.Address).HasMaxLength(500);
        builder.Property(warehouse => warehouse.IsActive);

        builder.Property(warehouse => warehouse.CreatedBy).HasMaxLength(200);
        builder.Property(warehouse => warehouse.ModifiedBy).HasMaxLength(200);

        // Soft deleted (isActive=false) tersembunyi dari query default.
        builder.HasQueryFilter(warehouse => warehouse.IsActive);

        builder.UseXminConcurrencyToken();
    }
}

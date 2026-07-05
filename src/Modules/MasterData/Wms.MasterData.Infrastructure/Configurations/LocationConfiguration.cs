using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Configurations;

// Mapping Location
public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<Location> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("location");

        builder.HasKey(location => location.Id);
        builder.Property(location => location.Id)
            .HasConversion(id => id.Value, value => LocationId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(location => location.WarehouseId)
            .HasConversion(id => id.Value, value => WarehouseId.Create(value).Value);
        builder.HasIndex(location => location.WarehouseId);

        builder.Property(location => location.Type).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(location => location.Code).HasMaxLength(64);
        builder.Property(location => location.IsActive);

        builder.Property(location => location.CreatedBy).HasMaxLength(200);
        builder.Property(location => location.ModifiedBy).HasMaxLength(200);

        builder.HasQueryFilter(location => location.IsActive);

        builder.UseXminConcurrencyToken();
    }
}

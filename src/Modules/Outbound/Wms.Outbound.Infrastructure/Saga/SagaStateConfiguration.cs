using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.Outbound.Infrastructure.Saga;

// Mapping saga state di schema outbound (kolom sagaId, sagaType, state JSON, status, createdAt, updatedAt).
public sealed class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    private const int IdMaxLength = 200;

    private const int StatusMaxLength = 24;

    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("saga_state");

        builder.HasKey(saga => saga.SagaId);
        builder.Property(saga => saga.SagaId).HasMaxLength(IdMaxLength).ValueGeneratedNever();
        builder.Property(saga => saga.SagaType).HasMaxLength(IdMaxLength);
        builder.Property(saga => saga.State).HasColumnType("jsonb");
        builder.Property(saga => saga.Status).HasMaxLength(StatusMaxLength);
        builder.Property(saga => saga.CreatedAt);
        builder.Property(saga => saga.UpdatedAt);

        builder.HasIndex(saga => saga.SagaType);
    }
}

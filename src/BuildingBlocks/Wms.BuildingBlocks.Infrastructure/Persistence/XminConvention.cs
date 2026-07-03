using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Optimistic concurrency lewat Postgres xmin.
public static class XminConvention
{
    public static EntityTypeBuilder<TEntity> UseXminConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        // UseXminAsConcurrencyToken di Npgsql 8.
#pragma warning disable CS0618
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        return builder;
    }
}

using Microsoft.EntityFrameworkCore;
using Wms.Reporting.Abstractions;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Persistence;

// Implement IProjectionStore menggunakan EF Core dan PostgreSQL.Cloud: Cosmos/Firestore
internal sealed class PostgresProjectionStore(ReportingDbContext dbContext) : IProjectionStore
{
    public async Task<TProjection?> GetAsync<TProjection>(object[] key, CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(key);

        // Gunakan FindAsync agar projection yang sudah di track dalam DbContext yang sama tetap terbaca.
        return await dbContext.Set<TProjection>().FindAsync(key, cancellationToken);
    }

    public async Task IncrementAsync<TProjection>(
        object[] key,
        Func<TProjection> create,
        Action<TProjection> increment,
        CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(increment);

        var row = await GetAsync<TProjection>(key, cancellationToken);
        if (row is null)
        {
            row = create();
            dbContext.Set<TProjection>().Add(row);
        }

        increment(row);
    }

    public async Task TruncateAllAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Set<StockOnHandView>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<ReceivingSummary>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<DispatchSummary>().ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<OperatorActivity>().ExecuteDeleteAsync(cancellationToken);
    }
}

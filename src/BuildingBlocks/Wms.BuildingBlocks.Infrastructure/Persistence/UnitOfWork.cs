using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Application memanggil SaveChangesAsync dan hanya melihat Result. Konflik xmin di-translate sekali. Tanpa auto-retry.
public sealed class UnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private static readonly Error _concurrencyError = new(
        "concurrency.conflict",
        "Data diubah proses lain sejak dimuat; muat ulang lalu coba lagi.");

    // Translator EF free — dipakai seam maupun jalur lain agar caller tak pernah lihat tipe EF.
    public static async Task SaveChangesTranslatingConflictAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Konflik optimistic concurrency (xmin) saat commit.", ex);
        }
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesTranslatingConflictAsync(dbContext, cancellationToken);
            return Result.Success();
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Conflict(_concurrencyError);
        }
    }
}

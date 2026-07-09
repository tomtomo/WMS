using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Application memanggil SaveChangesAsync dan hanya melihat Result. Konflik xmin & unique-violation di-translate sekali. Tanpa auto-retry.
public sealed class UnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private static readonly Error _concurrencyError = new(
        "concurrency.conflict",
        "Data diubah proses lain sejak dimuat; muat ulang lalu coba lagi.");

    // 23505 = duplikat natural key
    private static readonly Error _uniqueViolationError = new(
        "naturalkey.conflict",
        "Natural key sudah dipakai");

    // Simpan perubahan sambil menerjemahkan error database ke error domain,
    // supaya caller tidak perlu tahu detail EF atau Npgsql.
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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new UniqueConstraintConflictException("Unique constraint (23505) dilanggar saat commit, kemungkinan duplikat natural-key.", ex);
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
        catch (UniqueConstraintConflictException)
        {
            return Result.Conflict(_uniqueViolationError);
        }
    }
}

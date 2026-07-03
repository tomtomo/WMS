using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Auditing;

namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Isi IAuditable otomatis saat SaveChanges
public sealed class AuditableInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var actor = currentUser.IsAuthenticated ? currentUser.UserId : ICurrentUser.SystemActor;
        var timestamp = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = actor;
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.ModifiedBy = actor;
                entry.Entity.ModifiedAt = timestamp;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedBy = actor;
                entry.Entity.ModifiedAt = timestamp;
            }
        }
    }
}

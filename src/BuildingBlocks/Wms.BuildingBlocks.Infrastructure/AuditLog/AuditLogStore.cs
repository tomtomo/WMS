using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.AuditLog;

// AuditLogBehavior berjalan sebelum commit bisnis, jadi scope terpisah ini bertahan walau transaksi
// bisnis rollback. Audit tak boleh hilang bersama kegagalan bisnis.
public sealed class AuditLogStore(IServiceScopeFactory scopeFactory) : IAuditLogStore
{
    public async Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DbContext>();

        // Ambil correlation id dari request yang sedang berjalan, kalau tersedia.
        var correlationId = scope.ServiceProvider.GetService<ICorrelationContext>()?.CorrelationId;

        context.Set<AuditLogRecord>().Add(new AuditLogRecord
        {
            Id = Guid.NewGuid(),
            Actor = entry.Actor,
            Action = entry.Action,
            OccurredAt = entry.OccurredAt,
            CorrelationId = correlationId,
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.Inbox;

// Idempotent consumer guard di DbContext modul. MarkProcessed hanya Add, lalu ikut commit transaksi bisnis yang sama.
public sealed class InboxGuard(DbContext dbContext, TimeProvider timeProvider) : IInboxGuard
{
    public Task<bool> HasProcessedAsync(
        Guid eventId,
        string handlerName,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<InboxRecord>()
            .AnyAsync(
                record => record.EventId == eventId && record.HandlerType == handlerName,
                cancellationToken);

    public Task MarkProcessedAsync(
        Guid eventId,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<InboxRecord>().Add(new InboxRecord
        {
            EventId = eventId,
            HandlerType = handlerName,
            ProcessedAt = timeProvider.GetUtcNow(),
        });
        return Task.CompletedTask;
    }
}

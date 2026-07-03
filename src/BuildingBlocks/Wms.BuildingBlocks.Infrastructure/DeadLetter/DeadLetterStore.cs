using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

// Simpan message ke infrastructure.dead_letter. Kolom attempt_count belum dibawa port StoreAsync sehingga tetap default 0.
public sealed class DeadLetterStore(DbContext dbContext, TimeProvider timeProvider) : IDeadLetterStore
{
    public async Task StoreAsync(
        string logicalName,
        string payload,
        string reason,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<DeadLetterRecord>().Add(new DeadLetterRecord
        {
            Id = Guid.NewGuid(),
            Source = logicalName,
            Payload = payload,
            Error = reason,
            DeadLetteredAt = timeProvider.GetUtcNow(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

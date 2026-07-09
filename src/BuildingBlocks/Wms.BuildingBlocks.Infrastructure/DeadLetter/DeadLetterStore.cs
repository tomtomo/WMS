using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

// Simpan message ke infrastructure.dead_letter.
public sealed class DeadLetterStore(DbContext dbContext, TimeProvider timeProvider) : IDeadLetterStore
{
    public async Task StoreAsync(
        string logicalName,
        string payload,
        string reason,
        int attemptCount,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<DeadLetterRecord>().Add(new DeadLetterRecord
        {
            Id = Guid.NewGuid(),
            Source = logicalName,
            Payload = payload,
            Error = reason,
            AttemptCount = attemptCount,
            DeadLetteredAt = timeProvider.GetUtcNow(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

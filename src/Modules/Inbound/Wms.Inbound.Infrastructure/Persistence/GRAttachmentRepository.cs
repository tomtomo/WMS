using Microsoft.EntityFrameworkCore;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Infrastructure.Persistence;

internal sealed class GRAttachmentRepository(InboundDbContext context) : IGRAttachmentRepository
{
    public Task AddAsync(GRAttachment attachment, CancellationToken cancellationToken = default)
    {
        context.Set<GRAttachment>().Add(attachment);
        return Task.CompletedTask;
    }

    // Query filter IsActive berlaku
    public Task<GRAttachment?> GetActiveAsync(GRAttachmentId id, CancellationToken cancellationToken = default) =>
        context.Set<GRAttachment>().FirstOrDefaultAsync(attachment => attachment.Id == id, cancellationToken);
}

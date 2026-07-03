namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Audit log operasional, terpisah dari transaksi bisnis
public interface IAuditLogStore
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

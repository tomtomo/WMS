namespace Wms.BuildingBlocks.Application.Abstractions;

// Audit operasional yang ditulis IAuditLogStore. Kontrak minimal (aktor, aksi, waktu). DTO, bukan port, jadi di luar namespace Ports.
public sealed record AuditLogEntry(string Actor, string Action, DateTimeOffset OccurredAt);

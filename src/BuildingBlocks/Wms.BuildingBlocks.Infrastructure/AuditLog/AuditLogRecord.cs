namespace Wms.BuildingBlocks.Infrastructure.AuditLog;

public sealed class AuditLogRecord
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? Payload { get; set; }
}

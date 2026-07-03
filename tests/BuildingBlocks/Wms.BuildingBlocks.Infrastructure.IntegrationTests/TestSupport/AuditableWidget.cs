using Wms.BuildingBlocks.Domain.Auditing;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;

public sealed class AuditableWidget : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

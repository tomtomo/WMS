namespace Wms.BuildingBlocks.Infrastructure.Inbox;

public sealed class InboxRecord
{
    public Guid EventId { get; set; }

    public string HandlerType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}

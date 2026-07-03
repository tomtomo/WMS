namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

public sealed class DeadLetterRecord
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset DeadLetteredAt { get; set; }
}

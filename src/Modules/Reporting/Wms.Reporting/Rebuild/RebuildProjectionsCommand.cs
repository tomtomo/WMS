using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Reporting.Rebuild;

// Rebuild projection dari event log
public sealed record RebuildProjectionsCommand(IReadOnlyList<ReplayEvent> Events);

// Satu event untuk replay
public sealed record ReplayEvent(IIntegrationEvent Event, DateTimeOffset OccurredAt);

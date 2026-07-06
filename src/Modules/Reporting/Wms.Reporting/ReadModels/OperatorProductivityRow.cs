namespace Wms.Reporting.ReadModels;

// Read DTO Operator Productivity per operator/periode.
public sealed record OperatorProductivityRow(Guid OperatorId, DateOnly Period, int PutawayCount, int PickCount);

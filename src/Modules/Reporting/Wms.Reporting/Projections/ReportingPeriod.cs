namespace Wms.Reporting.Projections;

// Bucket periode projection = hari kejadian event (UTC).
internal static class ReportingPeriod
{
    public static DateOnly From(DateTimeOffset occurredAt) => DateOnly.FromDateTime(occurredAt.UtcDateTime);
}

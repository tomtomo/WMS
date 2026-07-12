using Wms.BuildingBlocks.Application.Abstractions;

namespace Wms.Reporting.ReadModels;

// Ringkasan telemetry operasional per gudang, dikelompokkan berdasarkan jenis event dan operator.
public sealed record TelemetrySummary(
    Guid WarehouseId,
    int TotalEvents,
    IReadOnlyList<TelemetryEventTypeCount> ByEventType,
    IReadOnlyList<TelemetryOperatorCount> ByOperator)
{
    public static TelemetrySummary Build(Guid warehouseId, IReadOnlyList<OperationalTelemetryRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var byEventType = records
            .GroupBy(record => record.EventType)
            .Select(group => new TelemetryEventTypeCount(group.Key.ToString(), group.Count(), group.Sum(record => record.Quantity)))
            .OrderBy(count => count.EventType, StringComparer.Ordinal)
            .ToList();

        var byOperator = records
            .GroupBy(record => record.OperatorId)
            .Select(group => new TelemetryOperatorCount(group.Key, group.Count()))
            .OrderByDescending(count => count.Count)
            .ToList();

        return new TelemetrySummary(warehouseId, records.Count, byEventType, byOperator);
    }
}

public sealed record TelemetryEventTypeCount(string EventType, int Count, decimal? TotalQuantity);

public sealed record TelemetryOperatorCount(Guid? OperatorId, int Count);

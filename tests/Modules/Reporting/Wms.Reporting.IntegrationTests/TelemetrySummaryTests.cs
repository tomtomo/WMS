using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

// Pastikan ringkasan telemetry menghitung data berdasarkan jenis event dan operator.
public sealed class TelemetrySummaryTests
{
    private static readonly DateTimeOffset _now = new(2026, 7, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_counts_events_by_type_and_operator()
    {
        var warehouseId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var records = new[]
        {
            Record(warehouseId, operatorId, OperationalTelemetryEventType.ScanCompleted, 5m),
            Record(warehouseId, operatorId, OperationalTelemetryEventType.ScanCompleted, 3m),
            Record(warehouseId, operatorId: null, OperationalTelemetryEventType.PutawayCompleted, 10m),
        };

        var summary = TelemetrySummary.Build(warehouseId, records);

        summary.WarehouseId.Should().Be(warehouseId);
        summary.TotalEvents.Should().Be(3);
        summary.ByEventType.Should().ContainEquivalentOf(new TelemetryEventTypeCount("ScanCompleted", 2, 8m));
        summary.ByEventType.Should().ContainEquivalentOf(new TelemetryEventTypeCount("PutawayCompleted", 1, 10m));
        summary.ByOperator.Should().ContainEquivalentOf(new TelemetryOperatorCount(operatorId, 2));
        summary.ByOperator.Should().ContainEquivalentOf(new TelemetryOperatorCount(null, 1));
    }

    [Fact]
    public void Build_of_empty_window_is_zeroed()
    {
        var warehouseId = Guid.NewGuid();

        var summary = TelemetrySummary.Build(warehouseId, []);

        summary.TotalEvents.Should().Be(0);
        summary.ByEventType.Should().BeEmpty();
        summary.ByOperator.Should().BeEmpty();
    }

    private static OperationalTelemetryRecord Record(
        Guid warehouseId, Guid? operatorId, OperationalTelemetryEventType eventType, decimal quantity) =>
        new(_now, warehouseId, operatorId, eventType, Guid.NewGuid(), quantity);
}

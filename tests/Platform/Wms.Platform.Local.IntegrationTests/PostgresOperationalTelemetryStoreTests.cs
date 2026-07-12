using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Pastikan telemetry operasional dapat disimpan dan dibaca kembali per gudang dalam batas waktu yang ditentukan.
[Collection(PostgresCollection.Name)]
public sealed class PostgresOperationalTelemetryStoreTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset _epoch = new(2026, 7, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Appended_record_is_read_back_within_window()
    {
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), new MutableTimeProvider(_epoch));
        using (store)
        {
            var warehouseId = Guid.NewGuid();
            var record = new OperationalTelemetryRecord(
                _epoch, warehouseId, Guid.NewGuid(), OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), 8m);
            await store.AppendAsync(record);

            var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromHours(1));

            recent.Should().ContainSingle();
            recent[0].Should().BeEquivalentTo(record);
        }
    }

    [Fact]
    public async Task Null_operator_and_quantity_round_trip()
    {
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), new MutableTimeProvider(_epoch));
        using (store)
        {
            var warehouseId = Guid.NewGuid();
            var record = new OperationalTelemetryRecord(
                _epoch, warehouseId, OperatorId: null, OperationalTelemetryEventType.PickCompleted, Guid.NewGuid(), Quantity: null);
            await store.AppendAsync(record);

            var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromHours(1));

            recent.Should().ContainSingle();
            recent[0].OperatorId.Should().BeNull();
            recent[0].Quantity.Should().BeNull();
        }
    }

    [Fact]
    public async Task Records_older_than_the_window_are_excluded()
    {
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), new MutableTimeProvider(_epoch));
        using (store)
        {
            var warehouseId = Guid.NewGuid();
            await store.AppendAsync(Record(warehouseId, _epoch.AddHours(-2)));
            await store.AppendAsync(Record(warehouseId, _epoch));

            var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromHours(1));

            recent.Should().ContainSingle("hanya record dalam window 1 jam");
            recent[0].OccurredAt.Should().Be(_epoch);
        }
    }

    [Fact]
    public async Task Window_larger_than_max_is_clamped_server_side()
    {
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), new MutableTimeProvider(_epoch));
        using (store)
        {
            var warehouseId = Guid.NewGuid();
            await store.AppendAsync(Record(warehouseId, _epoch.AddDays(-10)));

            // 100 hari di-clamp ke 7 hari → record 10 hari lalu tetap di luar window.
            var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromDays(100));

            recent.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Only_the_requested_warehouse_is_returned()
    {
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), new MutableTimeProvider(_epoch));
        using (store)
        {
            var warehouseA = Guid.NewGuid();
            var warehouseB = Guid.NewGuid();
            await store.AppendAsync(Record(warehouseA, _epoch));
            await store.AppendAsync(Record(warehouseB, _epoch));

            var recent = await store.GetRecentAsync(warehouseA, TimeSpan.FromHours(1));

            recent.Should().ContainSingle().Which.WarehouseId.Should().Be(warehouseA);
        }
    }

    private static OperationalTelemetryRecord Record(Guid warehouseId, DateTimeOffset occurredAt) =>
        new(occurredAt, warehouseId, Guid.NewGuid(), OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), 1m);
}

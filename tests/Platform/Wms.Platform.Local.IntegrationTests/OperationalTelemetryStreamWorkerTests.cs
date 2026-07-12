using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Wms.Platform.Local.Streaming;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Pastikan worker Local membaca telemetry dari stream in-memory lalu menyimpannya ke hot store.
[Collection(PostgresCollection.Name)]
public sealed class OperationalTelemetryStreamWorkerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Drain_moves_published_records_from_stream_to_store()
    {
        var ring = new InProcStreamRing();
        var publisher = new InProcStreamPublisher(ring);
        var consumer = new InProcStreamConsumer(ring);
        var store = new PostgresOperationalTelemetryStore(await fixture.CreateFreshDatabaseAsync(), TimeProvider.System);
        using (store)
        {
            var worker = new OperationalTelemetryStreamWorker(consumer, store, NullLogger<OperationalTelemetryStreamWorker>.Instance);
            var warehouseId = Guid.NewGuid();
            var record = new OperationalTelemetryRecord(
                DateTimeOffset.UtcNow, warehouseId, Guid.NewGuid(), OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), 3m);
            await publisher.PublishAsync(OperationalTelemetryStream.Name, record);

            await worker.DrainOnceAsync(CancellationToken.None);

            var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromHours(1));
            recent.Should().ContainSingle().Which.EntityId.Should().Be(record.EntityId);
        }
    }
}

using AwesomeAssertions;
using Wms.Reporting.Consumers;
using Wms.Reporting.IntegrationTests.TestSupport;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

// satu StockRemoved menambah DispatchSummary dan mengurangi StockOnHandView.
[Collection(PostgresCollection.Name)]
public sealed class StockRemovedProjectionTests(PostgresFixture postgres) : ReportingProjectionTestBase(postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _period = DateOnly.FromDateTime(_occurredAt.UtcDateTime);

    [Fact]
    public async Task One_stock_removed_increments_dispatch_and_decrements_on_hand()
    {
        var warehouseId = Guid.NewGuid();

        // Seed on hand 100 lewat GRConfirmed.
        var gr = SampleEvents.GrConfirmed(warehouseId, Guid.NewGuid(), [SampleEvents.Received("SKU-MILK", 100m, "B1")]);
        await DeliverAsync<StockOnHandFromReceiptConsumer>(consumer => consumer.ConsumeAsync(gr, Guid.NewGuid()));

        // Dispatch 30.
        var removed = SampleEvents.Removed(warehouseId, "SKU-MILK", 30m, "B1");
        (await DeliverAsync<StockRemovedConsumer>(consumer => consumer.ConsumeAsync(removed, Guid.NewGuid(), _occurredAt)))
            .IsSuccess.Should().BeTrue();

        var dispatch = await QueryAsync(db => db.Set<DispatchSummary>().FindAsync(warehouseId, _period).AsTask());
        dispatch.Should().NotBeNull();
        dispatch!.DispatchedVolume.Should().Be(30m);
        dispatch.WaveThroughput.Should().Be(1);

        // Konservasi: on hand 100 − 30 = 70.
        var stock = await QueryAsync(db => db.Set<StockOnHandView>().FindAsync(warehouseId, "SKU-MILK", "B1").AsTask());
        stock.Should().NotBeNull();
        stock!.QtyOnHand.Should().Be(70m);
    }
}

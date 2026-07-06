using AwesomeAssertions;
using Wms.Reporting.Consumers;
using Wms.Reporting.IntegrationTests.TestSupport;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class IdempotentNoDoubleCountTests(PostgresFixture postgres) : ReportingProjectionTestBase(postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _period = DateOnly.FromDateTime(_occurredAt.UtcDateTime);

    [Fact]
    public async Task Redelivered_same_event_does_not_double_count()
    {
        var supplierId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var gr = SampleEvents.GrConfirmed(Guid.NewGuid(), supplierId, [SampleEvents.Received("SKU-MILK", 100m, "B1")]);

        // Dua kali dengan eventId sama
        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, eventId, _occurredAt));
        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, eventId, _occurredAt));

        var summary = await QueryAsync(db => db.Set<ReceivingSummary>().FindAsync(supplierId, _period).AsTask());
        summary.Should().NotBeNull();
        summary!.ReceivedQty.Should().Be(100m);
        summary.ReceiptCount.Should().Be(1);
    }

    [Fact]
    public async Task Fan_out_processes_each_handler_type_once_despite_redelivery()
    {
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var gr = SampleEvents.GrConfirmed(warehouseId, supplierId, [SampleEvents.Received("SKU-MILK", 100m, "B1")]);

        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, eventId, _occurredAt));
        await DeliverAsync<StockOnHandFromReceiptConsumer>(consumer => consumer.ConsumeAsync(gr, eventId));
        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, eventId, _occurredAt));
        await DeliverAsync<StockOnHandFromReceiptConsumer>(consumer => consumer.ConsumeAsync(gr, eventId));

        var summary = await QueryAsync(db => db.Set<ReceivingSummary>().FindAsync(supplierId, _period).AsTask());
        summary!.ReceiptCount.Should().Be(1);

        var stock = await QueryAsync(db => db.Set<StockOnHandView>().FindAsync(warehouseId, "SKU-MILK", "B1").AsTask());
        stock!.QtyOnHand.Should().Be(100m);
    }
}

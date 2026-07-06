using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Consumers;
using Wms.Reporting.IntegrationTests.TestSupport;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

// satu GRConfirmed mengupdate ReceivingSummary dan StockOnHandView lewat dua handler berbeda
// handlerType Inbox, keduanya commit.
[Collection(PostgresCollection.Name)]
public sealed class GRConfirmedFanOutProjectionTests(PostgresFixture postgres) : ReportingProjectionTestBase(postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _period = DateOnly.FromDateTime(_occurredAt.UtcDateTime);

    [Fact]
    public async Task One_gr_confirmed_updates_two_projections_via_two_handler_types()
    {
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var gr = SampleEvents.GrConfirmed(warehouseId, supplierId, [SampleEvents.Received("SKU-MILK", 100m, "B1")]);

        (await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, eventId, _occurredAt)))
            .IsSuccess.Should().BeTrue();
        (await DeliverAsync<StockOnHandFromReceiptConsumer>(consumer => consumer.ConsumeAsync(gr, eventId)))
            .IsSuccess.Should().BeTrue();

        var summary = await QueryAsync(db => db.Set<ReceivingSummary>().FindAsync(supplierId, _period).AsTask());
        summary.Should().NotBeNull();
        summary!.ReceivedQty.Should().Be(100m);
        summary.ReceiptCount.Should().Be(1);
        summary.DiscrepancyCount.Should().Be(0);

        var stock = await QueryAsync(db => db.Set<StockOnHandView>().FindAsync(warehouseId, "SKU-MILK", "B1").AsTask());
        stock.Should().NotBeNull();
        stock!.QtyOnHand.Should().Be(100m);

        // Dua handlerType berbeda menghasilkan dua baris Inbox untuk eventId yang sama.
        var inboxRows = await QueryAsync(db => db.Set<InboxRecord>().CountAsync(record => record.EventId == eventId));
        inboxRows.Should().Be(2);
    }

    [Fact]
    public async Task Supplier_discrepancy_rate_is_served_via_read_port()
    {
        var supplierId = Guid.NewGuid();

        // Dua receipt supplier sama: satu bersih, satu punya rejectedLine sehingga rate menjadi 0.5.
        var clean = SampleEvents.GrConfirmed(Guid.NewGuid(), supplierId, [SampleEvents.Received("SKU-MILK", 100m, "B1")]);
        var discrepant = SampleEvents.GrConfirmed(
            Guid.NewGuid(),
            supplierId,
            [SampleEvents.Received("SKU-MILK", 90m, "B2")],
            [SampleEvents.Rejected("SKU-MILK", 10m)]);

        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(clean, Guid.NewGuid(), _occurredAt));
        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(discrepant, Guid.NewGuid(), _occurredAt));

        var page = await ScopedAsync(provider =>
            provider.GetRequiredService<IReceivingSummaryReader>().ListAsync(supplierId, 1, 20));

        page.Items.Should().ContainSingle();
        var row = page.Items[0];
        row.ReceiptCount.Should().Be(2);
        row.DiscrepancyCount.Should().Be(1);
        row.ReceivedQty.Should().Be(190m);
        row.DiscrepancyRate.Should().Be(0.5m);
    }
}

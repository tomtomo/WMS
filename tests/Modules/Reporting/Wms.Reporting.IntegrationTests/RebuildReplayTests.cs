using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Reporting.Consumers;
using Wms.Reporting.IntegrationTests.TestSupport;
using Wms.Reporting.ReadModels;
using Wms.Reporting.Rebuild;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RebuildReplayTests(PostgresFixture postgres) : ReportingProjectionTestBase(postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Rebuild_reproduces_identical_state_and_is_idempotent()
    {
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var gr = SampleEvents.GrConfirmed(
            warehouseId,
            supplierId,
            [SampleEvents.Received("SKU-MILK", 100m, "B1")],
            [SampleEvents.Rejected("SKU-MILK", 5m)]);
        var removed = SampleEvents.Removed(warehouseId, "SKU-MILK", 30m, "B1");
        var putaway = SampleEvents.Putaway(warehouseId, "SKU-MILK", operatorId);

        await DeliverAsync<ReceivingSummaryConsumer>(consumer => consumer.ConsumeAsync(gr, Guid.NewGuid(), _occurredAt));
        await DeliverAsync<StockOnHandFromReceiptConsumer>(consumer => consumer.ConsumeAsync(gr, Guid.NewGuid()));
        await DeliverAsync<StockRemovedConsumer>(consumer => consumer.ConsumeAsync(removed, Guid.NewGuid(), _occurredAt));
        await DeliverAsync<PutawayCompletedConsumer>(consumer => consumer.ConsumeAsync(putaway, Guid.NewGuid(), _occurredAt));

        var incremental = await SnapshotAsync();

        // Rebuild dari event stream yang sama
        var events = new List<ReplayEvent>
        {
            new(gr, _occurredAt),
            new(removed, _occurredAt),
            new(putaway, _occurredAt),
        };
        var command = new RebuildProjectionsCommand(events);

        (await ScopedAsync(provider =>
            provider.GetRequiredService<RebuildProjectionsHandler>().HandleAsync(command)))
            .IsSuccess.Should().BeTrue();
        var afterFirstRebuild = await SnapshotAsync();
        afterFirstRebuild.Should().BeEquivalentTo(incremental);

        // Rebuild kedua, tetap identik, idempotent.
        (await ScopedAsync(provider =>
            provider.GetRequiredService<RebuildProjectionsHandler>().HandleAsync(command)))
            .IsSuccess.Should().BeTrue();
        var afterSecondRebuild = await SnapshotAsync();
        afterSecondRebuild.Should().BeEquivalentTo(incremental);
    }

    private Task<ProjectionSnapshot> SnapshotAsync() => QueryAsync(async db =>
    {
        var stock = await db.Set<StockOnHandView>().AsNoTracking()
            .OrderBy(view => view.WarehouseId).ThenBy(view => view.Sku).ThenBy(view => view.Batch).ToListAsync();
        var receiving = await db.Set<ReceivingSummary>().AsNoTracking()
            .OrderBy(summary => summary.SupplierId).ThenBy(summary => summary.Period).ToListAsync();
        var dispatch = await db.Set<DispatchSummary>().AsNoTracking()
            .OrderBy(summary => summary.WarehouseId).ThenBy(summary => summary.Period).ToListAsync();
        var operators = await db.Set<OperatorActivity>().AsNoTracking()
            .OrderBy(activity => activity.OperatorId).ThenBy(activity => activity.Period).ToListAsync();
        return new ProjectionSnapshot(stock, receiving, dispatch, operators);
    });

    private sealed record ProjectionSnapshot(
        List<StockOnHandView> StockOnHand,
        List<ReceivingSummary> Receiving,
        List<DispatchSummary> Dispatch,
        List<OperatorActivity> Operators);
}

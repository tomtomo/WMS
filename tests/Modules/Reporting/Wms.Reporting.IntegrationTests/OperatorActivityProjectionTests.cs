using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Reporting.Consumers;
using Wms.Reporting.IntegrationTests.TestSupport;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

// PutawayCompleted menambah putaway count dan PickingCompleted menambah pick count, per operator per periode dan terpisah.
[Collection(PostgresCollection.Name)]
public sealed class OperatorActivityProjectionTests(PostgresFixture postgres) : ReportingProjectionTestBase(postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _period = DateOnly.FromDateTime(_occurredAt.UtcDateTime);

    [Fact]
    public async Task Putaway_and_pick_counts_are_attributed_separately_per_operator()
    {
        var operatorId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await DeliverAsync<PutawayCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.Putaway(warehouseId, "SKU-MILK", operatorId), Guid.NewGuid(), _occurredAt));
        await DeliverAsync<PutawayCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.Putaway(warehouseId, "SKU-RICE", operatorId), Guid.NewGuid(), _occurredAt));
        await DeliverAsync<PickingCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.Picking("SKU-MILK", 5m, operatorId, "B1"), Guid.NewGuid(), _occurredAt));

        var activity = await QueryAsync(db => db.Set<OperatorActivity>().FindAsync(operatorId, _period).AsTask());
        activity.Should().NotBeNull();
        activity!.PutawayCount.Should().Be(2);
        activity.PickCount.Should().Be(1);
    }

    [Fact]
    public async Task Null_operator_is_skipped_without_error()
    {
        var putaway = SampleEvents.Putaway(Guid.NewGuid(), "SKU-MILK", operatorId: null);

        var result = await DeliverAsync<PutawayCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(putaway, Guid.NewGuid(), _occurredAt));

        result.IsSuccess.Should().BeTrue();
        var rows = await QueryAsync(db => db.Set<OperatorActivity>().CountAsync());
        rows.Should().Be(0);
    }
}

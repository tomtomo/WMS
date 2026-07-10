using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestDoubles;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Contracts.Abstractions;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test IntegrationEventOutbox writer
[Collection(PostgresCollection.Name)]
public sealed class IntegrationEventOutboxTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Writes_an_outbox_row_in_the_business_transaction_without_saving_itself()
    {
        await using var context = await NewContextAsync();
        var outbox = new IntegrationEventOutbox(context, new FixedTimeProvider(_occurredAt));

        await outbox.AddAsync(new GoodsReceivedTestEvent("GR-9", 3), DeliveryClass.CoreFlow);

        // AddAsync hanya Add (anti dual-write) — belum tersimpan sampai transaksi bisnis commit.
        (await context.Set<OutboxRecord>().CountAsync()).Should().Be(0);

        await context.SaveChangesAsync();

        var row = await context.Set<OutboxRecord>().SingleAsync();
        row.LogicalName.Should().Be(GoodsReceivedTestEvent.LogicalName);
        row.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        row.OccurredAt.Should().Be(_occurredAt);
        row.Payload.Should().Contain("GR-9");
        row.Traceparent.Should().BeNull();
    }

    [Fact]
    public async Task Persists_the_partition_key_declared_by_the_contract()
    {
        await using var context = await NewContextAsync();
        var outbox = new IntegrationEventOutbox(context, new FixedTimeProvider(_occurredAt));
        var orderedEvent = new OrderedStreamTestEvent(Guid.NewGuid());

        await outbox.AddAsync(orderedEvent, DeliveryClass.CoreFlow);
        await context.SaveChangesAsync();

        var row = await context.Set<OutboxRecord>().SingleAsync();
        row.PartitionKey.Should().Be(((IHasPartitionKey)orderedEvent).PartitionKey);
    }

    [Fact]
    public async Task Leaves_partition_key_null_for_a_contract_without_ordering_need()
    {
        await using var context = await NewContextAsync();
        var outbox = new IntegrationEventOutbox(context, new FixedTimeProvider(_occurredAt));

        await outbox.AddAsync(new GoodsReceivedTestEvent("GR-7", 2), DeliveryClass.CoreFlow);
        await context.SaveChangesAsync();

        var row = await context.Set<OutboxRecord>().SingleAsync();
        row.PartitionKey.Should().BeNull();
    }

    private async Task<RailTestDbContext> NewContextAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}

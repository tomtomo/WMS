using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test InboxGuard
[Collection(PostgresCollection.Name)]
public sealed class InboxGuardTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_duplicate_delivery_of_the_same_key_is_seen_as_already_processed()
    {
        var connectionString = await CreateSchemaAsync();
        var eventId = Guid.NewGuid();

        // Delivery pertama.
        await using (var context = RailContext.New(connectionString))
        {
            var guard = new InboxGuard(context, TimeProvider.System);
            (await guard.HasProcessedAsync(eventId, "HandlerA")).Should().BeFalse();
            await guard.MarkProcessedAsync(eventId, "HandlerA");
            await context.SaveChangesAsync();
        }

        // Delivery duplikat.
        await using (var context = RailContext.New(connectionString))
        {
            var guard = new InboxGuard(context, TimeProvider.System);
            (await guard.HasProcessedAsync(eventId, "HandlerA")).Should().BeTrue();
        }
    }

    [Fact]
    public async Task The_composite_key_rejects_a_second_mark_from_a_racing_delivery()
    {
        var connectionString = await CreateSchemaAsync();
        var eventId = Guid.NewGuid();

        await using var first = RailContext.New(connectionString);
        await new InboxGuard(first, TimeProvider.System).MarkProcessedAsync(eventId, "HandlerA");
        await first.SaveChangesAsync();

        await using var racing = RailContext.New(connectionString);
        await new InboxGuard(racing, TimeProvider.System).MarkProcessedAsync(eventId, "HandlerA");
        var commit = async () => await racing.SaveChangesAsync();

        await commit.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Different_handlers_for_the_same_event_do_not_block_each_other()
    {
        var connectionString = await CreateSchemaAsync();
        var eventId = Guid.NewGuid();

        await using var context = RailContext.New(connectionString);
        var guard = new InboxGuard(context, TimeProvider.System);
        await guard.MarkProcessedAsync(eventId, "HandlerA");
        await guard.MarkProcessedAsync(eventId, "HandlerB");
        await context.SaveChangesAsync();

        (await guard.HasProcessedAsync(eventId, "HandlerA")).Should().BeTrue();
        (await guard.HasProcessedAsync(eventId, "HandlerB")).Should().BeTrue();
    }

    private async Task<string> CreateSchemaAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return connectionString;
    }
}

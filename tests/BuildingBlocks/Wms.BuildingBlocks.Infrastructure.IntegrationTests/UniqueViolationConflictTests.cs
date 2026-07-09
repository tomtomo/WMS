using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Memastikan unique violation dari Postgres berubah menjadi Result.Conflict, dan tidak tercampur dengan kasus concurrency conflict.
[Collection(PostgresCollection.Name)]
public sealed class UniqueViolationConflictTests(PostgresFixture postgres)
{
    private const string HandlerType = "HandlerA";

    [Fact]
    public async Task Translator_turns_a_unique_violation_into_an_ef_free_conflict()
    {
        var (connectionString, eventId) = await SeedInboxRowAsync();

        await using var racing = RailContext.New(connectionString);
        await new InboxGuard(racing, TimeProvider.System).MarkProcessedAsync(eventId, HandlerType);
        var conflictingCommit = async () => await UnitOfWork.SaveChangesTranslatingConflictAsync(racing);

        await conflictingCommit.Should().ThrowAsync<UniqueConstraintConflictException>();
    }

    [Fact]
    public async Task Unit_of_work_seam_maps_a_unique_violation_to_error_conflict()
    {
        var (connectionString, eventId) = await SeedInboxRowAsync();

        await using var racing = RailContext.New(connectionString);
        await new InboxGuard(racing, TimeProvider.System).MarkProcessedAsync(eventId, HandlerType);
        var result = await new UnitOfWork(racing).SaveChangesAsync();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("naturalkey.conflict");
    }

    // Row inbox pertama commit lewat context terpisah.
    private async Task<(string ConnectionString, Guid EventId)> SeedInboxRowAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        var eventId = Guid.NewGuid();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        await new InboxGuard(context, TimeProvider.System).MarkProcessedAsync(eventId, HandlerType);
        await context.SaveChangesAsync();
        return (connectionString, eventId);
    }
}

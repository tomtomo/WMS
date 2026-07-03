using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test translate konflik xmin
[Collection(PostgresCollection.Name)]
public sealed class ConcurrencyConflictTranslationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Translator_turns_ef_concurrency_exception_into_an_ef_free_conflict()
    {
        var (connectionString, id) = await SeedWidgetAsync();

        await using var first = RailContext.New(connectionString);
        await using var second = RailContext.New(connectionString);
        var fromFirst = await first.Set<WidgetEntity>().SingleAsync(widget => widget.Id == id);
        var fromSecond = await second.Set<WidgetEntity>().SingleAsync(widget => widget.Id == id);

        fromFirst.Name = "first-writer";
        await first.SaveChangesAsync();

        // Writer kedua memegang xmin lama
        fromSecond.Name = "second-writer";
        var conflictingCommit = async () => await UnitOfWork.SaveChangesTranslatingConflictAsync(second);

        await conflictingCommit.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Unit_of_work_seam_maps_the_conflict_to_error_conflict()
    {
        var (connectionString, id) = await SeedWidgetAsync();

        await using var first = RailContext.New(connectionString);
        await using var second = RailContext.New(connectionString);
        var fromFirst = await first.Set<WidgetEntity>().SingleAsync(widget => widget.Id == id);
        var fromSecond = await second.Set<WidgetEntity>().SingleAsync(widget => widget.Id == id);

        fromFirst.Name = "first-writer";
        await first.SaveChangesAsync();

        fromSecond.Name = "second-writer";
        var result = await new UnitOfWork(second).SaveChangesAsync();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("concurrency.conflict");
    }

    private async Task<(string ConnectionString, Guid Id)> SeedWidgetAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        var id = Guid.NewGuid();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        context.Set<WidgetEntity>().Add(new WidgetEntity { Id = id, Name = "seed" });
        await context.SaveChangesAsync();
        return (connectionString, id);
    }
}

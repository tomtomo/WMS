using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Azure.Persistence;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan perubahan projection memicu invalidasi cache melalui change feed, termasuk waktu tunda pemrosesannya.
public sealed class CosmosChangeFeedTests
{
    [Fact]
    public async Task Downstream_handler_invalidates_the_cache_entry_of_every_changed_projection()
    {
        var cacheStore = Substitute.For<ICacheStore>();
        var handler = new CacheInvalidatingProjectionChangeHandler(
            cacheStore,
            NullLogger<CacheInvalidatingProjectionChangeHandler>.Instance);
        var change = new ProjectionChange("Wms.Reporting.StockOnHandView", "wh|SKU-1|B1", DateTimeOffset.UtcNow, TimeSpan.Zero);

        await handler.HandleAsync([change]);

        await cacheStore.Received(1).RemoveAsync(
            "projection:Wms.Reporting.StockOnHandView:wh|SKU-1|B1",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_change_batch_is_ignored_instead_of_failing_the_lease()
    {
        var handler = new RecordingProjectionChangeHandler();
        var options = new CosmosOptions { AccountEndpoint = null };
        using var client = CosmosClientFactory.CreateWithConnectionString(
            "AccountEndpoint=https://localhost:8081/;AccountKey=Q29zbW9zRW11bGF0b3JLZXlGb3JVbml0VGVzdA==;",
            options);
        var processor = new CosmosChangeFeedProcessor(
            client,
            Options.Create(options),
            handler,
            TimeProvider.System,
            NullLogger<CosmosChangeFeedProcessor>.Instance);

        // Pastikan batch kosong tidak menyebabkan error yang dapat menghambat penyimpanan checkpoint change feed.
        await processor.OnChangesAsync([], CancellationToken.None);

        handler.Changes.Should().BeEmpty();
    }
}

[Collection(CosmosCollection.Name)]
public sealed class CosmosChangeFeedLiveTests(CosmosFixture fixture)
{
    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Projection_write_reaches_the_change_feed_within_the_lag_threshold()
    {
        Skip.IfNot(fixture.IsAvailable, "Cosmos live tak dikonfigurasi (WMS_PARITY_COSMOS_CONN).");

        var handler = new RecordingProjectionChangeHandler();
        var processor = new CosmosChangeFeedProcessor(
            fixture.Client!,
            fixture.AsOptions(),
            handler,
            TimeProvider.System,
            NullLogger<CosmosChangeFeedProcessor>.Instance);

        await processor.StartAsync(CancellationToken.None);
        try
        {
            var key = $"{Guid.NewGuid():N}|SKU-CF|B1";
            await fixture.CreateStore().UpsertAsync(key, new StockOnHandView { Sku = "SKU-CF", QtyOnHand = 3m });

            await ParityWait.UntilAsync(
                () => handler.Changes.Any(change => change.Key == key),
                TimeSpan.FromSeconds(90),
                "tulisan projection terbaca change feed");

            var observed = handler.Changes.First(change => change.Key == key);
            observed.ProjectionType.Should().Be(typeof(StockOnHandView).FullName);
            observed.Lag.Should().BePositive().And.BeLessThan(fixture.Options.ChangeFeedLagThreshold);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }
}

internal sealed class RecordingProjectionChangeHandler : IProjectionChangeHandler
{
    private readonly ConcurrentBag<ProjectionChange> _changes = [];

    public IReadOnlyCollection<ProjectionChange> Changes => _changes;

    public Task HandleAsync(IReadOnlyCollection<ProjectionChange> changes, CancellationToken cancellationToken = default)
    {
        foreach (var change in changes)
        {
            _changes.Add(change);
        }

        return Task.CompletedTask;
    }
}

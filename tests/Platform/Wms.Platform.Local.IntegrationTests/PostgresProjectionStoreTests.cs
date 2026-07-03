using AwesomeAssertions;
using Npgsql;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresProjectionStoreTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Missing_projection_returns_null()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresProjectionStore(dataSource, new MutableTimeProvider(Epoch));

            (await store.GetAsync<StockOnHandView>("sku-404")).Should().BeNull();
        }
    }

    [Fact]
    public async Task Upsert_then_get_round_trips_document()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresProjectionStore(dataSource, new MutableTimeProvider(Epoch));
            var view = new StockOnHandView("SKU-7", 120, 30);

            await store.UpsertAsync("SKU-7", view);

            (await store.GetAsync<StockOnHandView>("SKU-7")).Should().Be(view);
        }
    }

    [Fact]
    public async Task Rebuild_upserts_same_key_and_latest_wins()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresProjectionStore(dataSource, new MutableTimeProvider(Epoch));
            await store.UpsertAsync("SKU-9", new StockOnHandView("SKU-9", 10, 0));

            await store.UpsertAsync("SKU-9", new StockOnHandView("SKU-9", 55, 5));

            (await store.GetAsync<StockOnHandView>("SKU-9")).Should().Be(new StockOnHandView("SKU-9", 55, 5));
        }
    }

    [Fact]
    public async Task Same_key_under_different_projection_types_do_not_collide()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresProjectionStore(dataSource, new MutableTimeProvider(Epoch));
            await store.UpsertAsync("key-1", new StockOnHandView("SKU-1", 1, 0));

            await store.UpsertAsync("key-1", new PingPayload("bukan-stock"));

            (await store.GetAsync<StockOnHandView>("key-1"))!.Sku.Should().Be("SKU-1");
            (await store.GetAsync<PingPayload>("key-1"))!.Message.Should().Be("bukan-stock");
        }
    }

    private sealed record StockOnHandView(string Sku, int OnHand, int Reserved);
}

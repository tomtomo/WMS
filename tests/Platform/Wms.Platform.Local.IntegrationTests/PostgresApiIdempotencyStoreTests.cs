using AwesomeAssertions;
using Npgsql;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresApiIdempotencyStoreTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Unknown_key_is_a_miss()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(Epoch));

            (await store.GetResponseAsync("idem-unknown")).Should().BeNull();
        }
    }

    [Fact]
    public async Task Saved_response_is_a_hit_within_ttl()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(Epoch));

            await store.SaveResponseAsync("idem-1", """{"grId":7}""", TimeSpan.FromHours(1));

            (await store.GetResponseAsync("idem-1")).Should().Be("""{"grId":7}""");
        }
    }

    [Fact]
    public async Task Expired_entry_is_a_miss()
    {
        var clock = new MutableTimeProvider(Epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            await store.SaveResponseAsync("idem-2", "resp", TimeSpan.FromMinutes(30));

            clock.Advance(TimeSpan.FromMinutes(31));

            (await store.GetResponseAsync("idem-2")).Should().BeNull();
        }
    }

    [Fact]
    public async Task Live_entry_is_first_wins_on_replayed_save()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(Epoch));
            await store.SaveResponseAsync("idem-3", "respons-pertama", TimeSpan.FromHours(1));

            await store.SaveResponseAsync("idem-3", "respons-retry", TimeSpan.FromHours(1));

            (await store.GetResponseAsync("idem-3")).Should().Be("respons-pertama");
        }
    }

    [Fact]
    public async Task Expired_entry_can_be_replaced_by_new_save()
    {
        var clock = new MutableTimeProvider(Epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            await store.SaveResponseAsync("idem-4", "lama", TimeSpan.FromMinutes(30));

            clock.Advance(TimeSpan.FromMinutes(31));
            await store.SaveResponseAsync("idem-4", "baru", TimeSpan.FromMinutes(30));

            (await store.GetResponseAsync("idem-4")).Should().Be("baru");
        }
    }
}

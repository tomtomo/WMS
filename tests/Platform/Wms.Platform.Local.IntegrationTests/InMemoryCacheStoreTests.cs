using AwesomeAssertions;
using Wms.Platform.Local.Cache;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

public sealed class InMemoryCacheStoreTests
{
    private static readonly DateTimeOffset _epoch = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

    private readonly MutableTimeProvider _clock = new(_epoch);
    private readonly InMemoryCacheStore _store;

    public InMemoryCacheStoreTests() => _store = new InMemoryCacheStore(_clock);

    [Fact]
    public async Task Set_then_get_within_ttl_returns_value()
    {
        await _store.SetAsync("masterdata:product:1", "Widget", TimeSpan.FromMinutes(5));

        var value = await _store.GetAsync<string>("masterdata:product:1");

        value.Should().Be("Widget");
    }

    [Fact]
    public async Task Get_after_absolute_ttl_passed_is_miss()
    {
        await _store.SetAsync("masterdata:product:2", "Widget", TimeSpan.FromMinutes(5));
        _clock.Advance(TimeSpan.FromMinutes(6));

        var value = await _store.GetAsync<string>("masterdata:product:2");

        value.Should().BeNull();
    }

    [Fact]
    public async Task Null_value_is_never_cached()
    {
        await _store.SetAsync<string?>("masterdata:product:3", null, TimeSpan.FromMinutes(5));

        var value = await _store.GetAsync<string>("masterdata:product:3");

        value.Should().BeNull();
    }

    [Fact]
    public async Task Non_positive_ttl_is_never_cached()
    {
        await _store.SetAsync("masterdata:product:4", "Widget", TimeSpan.Zero);

        var value = await _store.GetAsync<string>("masterdata:product:4");

        value.Should().BeNull();
    }

    [Fact]
    public async Task Remove_evicts_entry()
    {
        await _store.SetAsync("masterdata:product:5", "Widget", TimeSpan.FromMinutes(5));

        await _store.RemoveAsync("masterdata:product:5");

        (await _store.GetAsync<string>("masterdata:product:5")).Should().BeNull();
    }
}

using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Local.Cache;
using Wms.Platform.Shared.Cache;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan cache aside memakai key masterdata:{type}:{id}, masa berlaku 5 menit, dan tidak menyimpan nilai null.
// Test yang sama dijalankan untuk InMemory dan Redis. (Managed Redis / Memorystore).
public abstract class CacheStoreParityTests
{
    private static readonly TimeSpan _masterDataTimeToLive = TimeSpan.FromMinutes(5);

    // Gunakan masa berlaku singkat agar test Redis tidak perlu menunggu terlalu lama.
    private static readonly TimeSpan _shortTimeToLive = TimeSpan.FromSeconds(1);

    protected abstract ICacheStore Store { get; }

    [Fact]
    public async Task Missing_key_reads_as_null()
    {
        var value = await Store.GetAsync<Product>(NewKey());

        value.Should().BeNull();
    }

    [Fact]
    public async Task Cached_value_round_trips_within_its_ttl()
    {
        var key = NewKey();
        var product = new Product("SKU-1", "Palet kayu");

        await Store.SetAsync(key, product, _masterDataTimeToLive);

        (await Store.GetAsync<Product>(key)).Should().Be(product);
    }

    [Fact]
    public async Task Null_value_is_never_cached()
    {
        var key = NewKey();

        await Store.SetAsync<Product?>(key, null, _masterDataTimeToLive);

        (await Store.GetAsync<Product>(key)).Should().BeNull();
    }

    [Fact]
    public async Task Non_positive_ttl_is_never_cached()
    {
        var key = NewKey();

        await Store.SetAsync(key, new Product("SKU-2", "Krat"), TimeSpan.Zero);

        (await Store.GetAsync<Product>(key)).Should().BeNull();
    }

    [Fact]
    public async Task Entry_disappears_once_its_ttl_elapses()
    {
        var key = NewKey();
        await Store.SetAsync(key, new Product("SKU-3", "Drum"), _shortTimeToLive);

        await ElapsePastTimeToLiveAsync(_shortTimeToLive);

        (await Store.GetAsync<Product>(key)).Should().BeNull();
    }

    [Fact]
    public async Task Removed_entry_is_gone_before_its_ttl()
    {
        var key = NewKey();
        await Store.SetAsync(key, new Product("SKU-4", "Rak"), _masterDataTimeToLive);

        await Store.RemoveAsync(key);

        (await Store.GetAsync<Product>(key)).Should().BeNull();
    }

    [Fact]
    public async Task Entry_of_another_shape_reads_as_a_miss_instead_of_throwing()
    {
        var key = NewKey();
        await Store.SetAsync(key, "bukan-product", _masterDataTimeToLive);

        // Cache aside tetap mengembalikan null jika format data lama tidak sesuai, tanpa menyebabkan error.
        (await Store.GetAsync<Product>(key)).Should().BeNull();
    }

    [Fact]
    public async Task Blank_key_is_rejected_at_the_boundary()
    {
        var get = () => Store.GetAsync<Product>(" ");

        await get.Should().ThrowAsync<ArgumentException>();
    }

    // Redis menghitung masa berlaku di server, sedangkan InMemory menggunakan TimeProvider.
    protected abstract Task ElapsePastTimeToLiveAsync(TimeSpan timeToLive);

    private static string NewKey() => $"masterdata:product:{Guid.NewGuid():N}";

    // Bentuk value cache Master Data yang disederhanakan.
    protected sealed record Product(string Sku, string Name);
}

public sealed class InMemoryCacheStoreParityTests : CacheStoreParityTests
{
    private readonly FakeTimeProvider _timeProvider = new();

    public InMemoryCacheStoreParityTests() => Store = new InMemoryCacheStore(_timeProvider);

    protected override ICacheStore Store { get; }

    protected override Task ElapsePastTimeToLiveAsync(TimeSpan timeToLive)
    {
        _timeProvider.Advance(timeToLive + TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }
}

[Collection(RedisCollection.Name)]
public sealed class RedisCacheStoreParityTests : CacheStoreParityTests
{
    public RedisCacheStoreParityTests(RedisFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Store = new RedisCacheStore(fixture.Multiplexer);
    }

    protected override ICacheStore Store { get; }

    // Masa berlaku Redis dihitung oleh server, jadi test perlu menunggu sampai waktunya benar-benar habis.
    protected override Task ElapsePastTimeToLiveAsync(TimeSpan timeToLive) =>
        Task.Delay(timeToLive + TimeSpan.FromMilliseconds(500));
}

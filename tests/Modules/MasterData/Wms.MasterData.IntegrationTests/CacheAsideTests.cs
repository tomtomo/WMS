using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;
using Wms.MasterData.Infrastructure.Persistence.Cached;
using Wms.Platform.Local.Cache;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

// Cache aside Decorator
public sealed class CacheAsideTests
{
    private const string Sku = "SKU-MILK";

    private static readonly ProductSnapshotDto _milk =
        new(Sku, "Fresh Milk 1L", "carton", true, true, false, 30, true);

    private static ProductSnapshotDto? NullSnapshot => null;

    [Fact]
    public async Task Second_lookup_returns_cached_value_without_touching_inner_reader()
    {
        var inner = Substitute.For<IProductReader>();
        inner.GetBySkuAsync(Sku, Arg.Any<CancellationToken>()).Returns(_milk);
        var reader = new CachedProductReader(inner, new InMemoryCacheStore(new FakeTimeProvider()));

        var first = await reader.GetBySkuAsync(Sku);
        var second = await reader.GetBySkuAsync(Sku);

        first.Should().Be(_milk);
        second.Should().Be(_milk);
        await inner.Received(1).GetBySkuAsync(Sku, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Not_found_lookups_are_never_cached_so_they_always_hit_inner()
    {
        var inner = Substitute.For<IProductReader>();
        inner.GetBySkuAsync("MISSING", Arg.Any<CancellationToken>()).Returns(NullSnapshot);
        var reader = new CachedProductReader(inner, new InMemoryCacheStore(new FakeTimeProvider()));

        await reader.GetBySkuAsync("MISSING");
        await reader.GetBySkuAsync("MISSING");

        await inner.Received(2).GetBySkuAsync("MISSING", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_key_is_masterdata_type_id()
    {
        var inner = Substitute.For<IProductReader>();
        inner.GetBySkuAsync(Sku, Arg.Any<CancellationToken>()).Returns(_milk);
        var cache = new InMemoryCacheStore(new FakeTimeProvider());
        var reader = new CachedProductReader(inner, cache);

        await reader.GetBySkuAsync(Sku);

        (await cache.GetAsync<ProductSnapshotDto>($"masterdata:product:{Sku}")).Should().Be(_milk);
    }

    [Fact]
    public async Task Cached_entry_expires_after_the_five_minute_ttl()
    {
        var inner = Substitute.For<IProductReader>();
        inner.GetBySkuAsync(Sku, Arg.Any<CancellationToken>()).Returns(_milk);
        var clock = new FakeTimeProvider();
        var reader = new CachedProductReader(inner, new InMemoryCacheStore(clock));

        await reader.GetBySkuAsync(Sku);
        clock.Advance(TimeSpan.FromMinutes(6));
        await reader.GetBySkuAsync(Sku);

        await inner.Received(2).GetBySkuAsync(Sku, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_queries_are_not_cached_and_pass_through_to_inner()
    {
        var inner = Substitute.For<IProductReader>();
        inner.ListAsync(1, 20, false, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductSnapshotDto>([_milk], 1, 1, 20));
        var reader = new CachedProductReader(inner, new InMemoryCacheStore(new FakeTimeProvider()));

        await reader.ListAsync(1, 20);
        await reader.ListAsync(1, 20);

        await inner.Received(2).ListAsync(1, 20, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Warehouse_decorator_caches_by_warehouse_key()
    {
        var id = Guid.NewGuid();
        var dto = new WarehouseDto(id, "DC Jakarta", "Jl. Cakung", true);
        var inner = Substitute.For<IWarehouseReader>();
        inner.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        var cache = new InMemoryCacheStore(new FakeTimeProvider());
        var reader = new CachedWarehouseReader(inner, cache);

        await reader.GetByIdAsync(id);
        await reader.GetByIdAsync(id);

        await inner.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
        (await cache.GetAsync<WarehouseDto>($"masterdata:warehouse:{id}")).Should().Be(dto);
    }

    [Fact]
    public async Task Location_decorator_caches_by_location_key()
    {
        var id = Guid.NewGuid();
        var dto = new LocationDto(id, Guid.NewGuid(), "Rack", "RACK-01", true);
        var inner = Substitute.For<ILocationReader>();
        inner.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        var cache = new InMemoryCacheStore(new FakeTimeProvider());
        var reader = new CachedLocationReader(inner, cache);

        await reader.GetByIdAsync(id);
        await reader.GetByIdAsync(id);

        await inner.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
        (await cache.GetAsync<LocationDto>($"masterdata:location:{id}")).Should().Be(dto);
    }
}

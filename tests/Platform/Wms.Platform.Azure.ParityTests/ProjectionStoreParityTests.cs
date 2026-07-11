using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan projection dapat dibuat ulang dan menghasilkan data yang sama di Postgres maupun Cosmos.
// Postgres untuk lokal, sedangkan Cosmos digunakan sebagai adapter cloud.
public abstract class ProjectionStoreParityTests
{
    protected abstract IProjectionStore Store { get; }

    [SkippableFact]
    public async Task Missing_projection_reads_as_null()
    {
        EnsureAdapterAvailable();

        (await Store.GetAsync<StockOnHandView>(NewKey())).Should().BeNull();
    }

    [SkippableFact]
    public async Task Upserted_projection_round_trips()
    {
        EnsureAdapterAvailable();
        var key = NewKey();
        var view = new StockOnHandView { WarehouseId = Guid.NewGuid(), Sku = "SKU-1", Batch = "B1", QtyOnHand = 12m };

        await Store.UpsertAsync(key, view);

        var stored = await Store.GetAsync<StockOnHandView>(key);
        stored.Should().BeEquivalentTo(view);
    }

    [SkippableFact]
    public async Task Upsert_overwrites_the_previous_document()
    {
        EnsureAdapterAvailable();
        var key = NewKey();
        await Store.UpsertAsync(key, new StockOnHandView { Sku = "SKU-1", QtyOnHand = 5m });

        await Store.UpsertAsync(key, new StockOnHandView { Sku = "SKU-1", QtyOnHand = 9m });

        (await Store.GetAsync<StockOnHandView>(key))!.QtyOnHand.Should().Be(9m);
    }

    [SkippableFact]
    public async Task Different_projection_shapes_do_not_collide_on_the_same_key()
    {
        EnsureAdapterAvailable();
        var key = NewKey();
        await Store.UpsertAsync(key, new StockOnHandView { Sku = "SKU-1", QtyOnHand = 5m });
        await Store.UpsertAsync(key, new ReceivingSummaryView { ReceivedLines = 3 });

        (await Store.GetAsync<StockOnHandView>(key))!.QtyOnHand.Should().Be(5m);
        (await Store.GetAsync<ReceivingSummaryView>(key))!.ReceivedLines.Should().Be(3);
    }

    [SkippableFact]
    public async Task Replaying_the_same_events_rebuilds_an_identical_projection()
    {
        EnsureAdapterAvailable();
        decimal[] receivedQuantities = [4m, 6m, -2m, 7m];
        var warehouseId = Guid.NewGuid();

        var first = await ReplayAsync(NewKey(), warehouseId, receivedQuantities);
        var rebuilt = await ReplayAsync(NewKey(), warehouseId, receivedQuantities);

        rebuilt.Should().BeEquivalentTo(first, "projection wajib rebuildable dari event yang sama");
        rebuilt.QtyOnHand.Should().Be(15m);
    }

    [SkippableFact]
    public async Task Blank_key_is_rejected_at_the_boundary()
    {
        EnsureAdapterAvailable();

        var get = () => Store.GetAsync<StockOnHandView>(" ");

        await get.Should().ThrowAsync<ArgumentException>();
    }

    [SkippableFact]
    public async Task Null_projection_is_rejected_at_the_boundary()
    {
        EnsureAdapterAvailable();

        var upsert = () => Store.UpsertAsync<StockOnHandView>(NewKey(), null!);

        await upsert.Should().ThrowAsync<ArgumentNullException>();
    }

    // Adapter Cosmos melewatkan test jika kredensial resource test belum dikonfigurasi.
    protected abstract void EnsureAdapterAvailable();

    private static string NewKey() => $"{Guid.NewGuid():N}|SKU-1|B1";

    // Terapkan ulang setiap event untuk memperbarui jumlah stok, lalu simpan projection terbaru.
    private async Task<StockOnHandView> ReplayAsync(string key, Guid warehouseId, decimal[] quantities)
    {
        foreach (var quantity in quantities)
        {
            var current = await Store.GetAsync<StockOnHandView>(key)
                ?? new StockOnHandView { WarehouseId = warehouseId, Sku = "SKU-1", Batch = "B1" };
            current.QtyOnHand += quantity;
            await Store.UpsertAsync(key, current);
        }

        return (await Store.GetAsync<StockOnHandView>(key))!;
    }
}

// Read model Reporting, disalin di sini karena Platform tidak boleh tau Modules.
public sealed class StockOnHandView
{
    public Guid WarehouseId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Batch { get; set; } = string.Empty;

    public decimal QtyOnHand { get; set; }
}

public sealed class ReceivingSummaryView
{
    public int ReceivedLines { get; set; }
}

[Collection(PostgresCollection.Name)]
public sealed class PostgresProjectionStoreParityTests : ProjectionStoreParityTests
{
    public PostgresProjectionStoreParityTests(PostgresFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Store = new PostgresProjectionStore(fixture.DataSource, TimeProvider.System);
    }

    protected override IProjectionStore Store { get; }

    protected override void EnsureAdapterAvailable()
    {
        // Adapter Local selalu tersedia karena menggunakan Testcontainers.
    }
}

[Collection(CosmosCollection.Name)]
public sealed class CosmosProjectionStoreParityTests : ProjectionStoreParityTests
{
    private readonly CosmosFixture _fixture;

    public CosmosProjectionStoreParityTests(CosmosFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        Store = fixture.IsAvailable ? fixture.CreateStore() : NullProjectionStore.Instance;
    }

    protected override IProjectionStore Store { get; }

    protected override void EnsureAdapterAvailable() =>
        Skip.IfNot(_fixture.IsAvailable, "Cosmos live tak dikonfigurasi (WMS_PARITY_COSMOS_CONN).");

    // Placeholder agar test tetap bisa dibuat saat kredensial Cosmos belum ada.
    // Implementasi ini tidak akan dipanggil karena test sudah lebih dulu diskip.
    private sealed class NullProjectionStore : IProjectionStore
    {
        public static readonly NullProjectionStore Instance = new();

        public Task UpsertAsync<TProjection>(string key, TProjection projection, CancellationToken cancellationToken = default)
            where TProjection : class => throw new InvalidOperationException("Cosmos live tak dikonfigurasi.");

        public Task<TProjection?> GetAsync<TProjection>(string key, CancellationToken cancellationToken = default)
            where TProjection : class => throw new InvalidOperationException("Cosmos live tak dikonfigurasi.");
    }
}

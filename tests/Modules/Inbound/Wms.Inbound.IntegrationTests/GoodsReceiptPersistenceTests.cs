using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Mapping EF
[Collection(PostgresCollection.Name)]
public sealed class GoodsReceiptPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InboundTestHost.Build(connectionString);
        await InboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Aggregate_roundtrip_semua_koleksi_dan_field()
    {
        var expiry = new DateOnly(2027, 3, 15);
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m), ("SKU-B", 5m));
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 12m, batch: "B1", expiry: expiry);
        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-B", 3m, LineStatus.QcHold);
        await GoodsReceiptScenarios.CompleteScanAsync(_provider, grId);
        await GoodsReceiptScenarios.ResolveAllAsync(_provider, grId);
        await PipelineRunner.SendAsync(
            _provider,
            new Application.Features.ConfirmGoodsReceipt.ConfirmGoodsReceiptCommand(grId));

        var detail = await GoodsReceiptScenarios.ReadDetailAsync(_provider, grId);

        detail.PoRef.Should().Be("PO-2026-001");
        detail.SupplierId.Should().Be(GoodsReceiptScenarios.SupplierId);
        detail.DockDoor.Should().Be("DOCK-1");
        detail.Status.Should().Be(nameof(GoodsReceiptStatus.Confirmed));
        detail.ExpectedLines.Should().HaveCount(2);
        detail.ScannedLines.Should().HaveCount(2);
        detail.ScannedLines.Should().ContainSingle(line => line.Batch == "B1" && line.Expiry == expiry);
        detail.QuantityChecks.Should().HaveCount(2);
        detail.Discrepancies.Should().HaveCount(3, "over SKU-A + short SKU-B + qc SKU-B");
        detail.Resolutions.Should().HaveCount(3);
        detail.ReceivedLines.Should().NotBeEmpty();
        detail.RejectedLines.Should().ContainSingle(line => line.Sku == "SKU-A");
    }

    [Fact]
    public async Task Audit_interceptor_mengisi_created_dan_modified()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));

        var (createdBy, createdAt) = await PipelineRunner.QueryDbAsync(_provider, async context =>
        {
            var gr = await context.Set<GoodsReceipt>().AsNoTracking()
                .FirstAsync(g => g.Id == GoodsReceiptId.Create(grId).Value);
            return (gr.CreatedBy, gr.CreatedAt);
        });

        createdBy.Should().Be(FixedCurrentUser.TestUserId);
        createdAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Migration_menempatkan_rail_di_schema_infrastructure_dan_modul_di_inbound()
    {
        var tables = await PipelineRunner.QueryDbAsync(_provider, async context =>
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = """
                    SELECT table_schema || '.' || table_name
                    FROM information_schema.tables
                    WHERE table_schema IN ('inbound', 'infrastructure')
                    """;
                var names = new List<string>();
                var reader = await command.ExecuteReaderAsync();
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync())
                    {
                        names.Add(reader.GetString(0));
                    }
                }

                return names;
            }
        });

        tables.Should().Contain(["infrastructure.outbox", "infrastructure.inbox", "infrastructure.dead_letter", "infrastructure.audit_log"]);
        tables.Should().Contain("inbound.goods_receipt");
        tables.Should().Contain("inbound.gr_attachment");
        tables.Should().Contain("inbound.__ef_migrations_history");
    }
}

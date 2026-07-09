using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Contracts.Abstractions;
using Wms.Inventory.Application.Features.DetectNearExpiry;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Detektor (bukan transisi state) menscan Stock Available/OnHand
// dengan expiry ≤ now dan threshold pakai TimeProvider
[Collection(PostgresCollection.Name)]
public sealed class DetectNearExpiryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string NearExpiryLogicalName = "inventory.stock_near_expiry.v1";

    private static readonly DateTimeOffset _today = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(_today);

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InventoryTestHost.Build(connectionString, services => services.AddSingleton<TimeProvider>(_clock));
        await InventoryTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Scan_emits_near_expiry_for_available_and_on_hand_within_threshold()
    {
        await StockSeeder.SeedAvailableAsync(_provider, batch: "LOT-A", expiry: new DateOnly(2026, 1, 20));
        await StockSeeder.SeedOnHandAsync(_provider, batch: "LOT-C", expiry: new DateOnly(2026, 1, 10));
        await StockSeeder.SeedAvailableAsync(_provider, batch: "LOT-FAR", expiry: new DateOnly(2026, 6, 30));

        var result = await PipelineRunner.SendAsync(_provider, new DetectNearExpiryCommand(ThresholdDays: 30));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, NearExpiryLogicalName);
        rows.Should().HaveCount(2, "hanya balance dalam ambang (LOT-A, LOT-C); LOT-FAR di luar");
        rows.Should().OnlyContain(row => row.DeliveryClass == DeliveryClass.Notification);

        var payloads = rows.Select(Payload).ToList();
        payloads.Single(payload => payload.Batch == "LOT-A").DaysToExpiry.Should().Be(19);
        payloads.Single(payload => payload.Batch == "LOT-C").DaysToExpiry.Should().Be(9);
        payloads.Should().NotContain(payload => payload.Batch == "LOT-FAR");
    }

    [Fact]
    public async Task Scan_does_not_change_stock_state_and_is_safe_to_rerun()
    {
        var stockId = await StockSeeder.SeedAvailableAsync(_provider, expiry: new DateOnly(2026, 1, 15));

        await PipelineRunner.SendAsync(_provider, new DetectNearExpiryCommand(30));
        await PipelineRunner.SendAsync(_provider, new DetectNearExpiryCommand(30));

        var stock = (await PipelineRunner.StocksAsync(_provider)).Single(candidate => candidate.Id.Value == stockId);
        stock.Status.Should().Be(StockStatus.Available, "scan = deteksi temporal, bukan transisi state");
        stock.Qty.Should().Be(100m);
    }

    [Fact]
    public async Task Scan_starts_emitting_once_clock_advances_past_threshold()
    {
        await StockSeeder.SeedAvailableAsync(_provider, batch: "LOT-A", expiry: new DateOnly(2026, 3, 1));

        await PipelineRunner.SendAsync(_provider, new DetectNearExpiryCommand(30));
        (await PipelineRunner.OutboxRowsAsync(_provider, NearExpiryLogicalName)).Should().BeEmpty(
            "2026-03-01 di luar ambang 30 hari dari 2026-01-01");

        _clock.Advance(TimeSpan.FromDays(40)); // now 2026-02-10 → threshold 2026-03-12

        await PipelineRunner.SendAsync(_provider, new DetectNearExpiryCommand(30));
        var rows = await PipelineRunner.OutboxRowsAsync(_provider, NearExpiryLogicalName);
        rows.Should().ContainSingle("clock lewat ambang → LOT-A kini mendekati kadaluarsa");
        Payload(rows[0]).DaysToExpiry.Should().Be(19, "2026-03-01 − 2026-02-10 = 19 hari");
    }

    private static StockNearExpiry Payload(OutboxRecord row) =>
        JsonSerializer.Deserialize<StockNearExpiry>(row.Payload, MessageEnvelope.PayloadSerializerOptions)!;
}

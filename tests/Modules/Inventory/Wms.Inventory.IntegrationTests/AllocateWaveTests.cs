using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.Infrastructure;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// consume WaveReleased, FEFO reserve, satu StockAllocationCompleted, idempotent
[Collection(PostgresCollection.Name)]
public sealed class AllocateWaveTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string AllocationLogicalName = "inventory.stock_allocation_completed.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InventoryTestHost.Build(connectionString);
        await InventoryTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Full_allocation_reserves_line_and_emits_fully_allocated_core_flow_only()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var orderId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(orderId, qty: 10m)), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var reservations = await PipelineRunner.ReservationsAsync(_provider);
        reservations.Should().ContainSingle();
        reservations[0].Status.Should().Be(ReservationStatus.Active);
        reservations[0].Qty.Should().Be(10m);

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName);
        rows.Should().ContainSingle("FullyAllocated tak punya shortfalls, hanya rail CoreFlow");
        rows[0].DeliveryClass.Should().Be(DeliveryClass.CoreFlow);

        var payload = Payload(rows[0]);
        payload.Status.Should().Be(AllocationStatus.FullyAllocated);
        payload.Allocations.Should().ContainSingle().Which.Qty.Should().Be(10m);
        payload.Shortfalls.Should().BeEmpty();
    }

    [Fact]
    public async Task Partial_allocation_emits_allocations_and_shortfalls_on_both_rails()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 8m);
        var orderId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(orderId, qty: 10m)), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName);
        rows.Select(row => row.DeliveryClass).Should().BeEquivalentTo(
            [DeliveryClass.CoreFlow, DeliveryClass.Notification], "shortfalls");

        var payload = Payload(rows[0]);
        payload.Status.Should().Be(AllocationStatus.PartiallyAllocated);
        payload.Allocations.Should().ContainSingle().Which.Qty.Should().Be(8m);
        var shortfall = payload.Shortfalls.Should().ContainSingle().Which;
        shortfall.RequestedQty.Should().Be(10m);
        shortfall.AllocatedQty.Should().Be(8m);
        shortfall.ShortQty.Should().Be(2m);
    }

    [Fact]
    public async Task Unfulfilled_when_no_stock_emits_empty_allocations_and_full_shortfall()
    {
        var orderId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(orderId, qty: 10m)), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        (await PipelineRunner.ReservationsAsync(_provider)).Should().BeEmpty("nol stok, tidak ada reservasi");

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName);
        rows.Select(row => row.DeliveryClass).Should().BeEquivalentTo([DeliveryClass.CoreFlow, DeliveryClass.Notification]);

        var payload = Payload(rows[0]);
        payload.Status.Should().Be(AllocationStatus.Unfulfilled);
        payload.Allocations.Should().BeEmpty();
        payload.Shortfalls.Should().ContainSingle().Which.ShortQty.Should().Be(10m);
    }

    [Fact]
    public async Task Fefo_consumes_nearest_expiry_batch_first()
    {
        var near = new DateOnly(2026, 6, 30);
        var far = new DateOnly(2026, 12, 31);
        await StockSeeder.SeedAvailableAsync(_provider, qty: 5m, batch: "LOT-FAR", expiry: far);
        await StockSeeder.SeedAvailableAsync(_provider, qty: 5m, batch: "LOT-NEAR", expiry: near);

        var result = await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(Guid.NewGuid(), qty: 8m)), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var payload = Payload((await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName))[0]);
        payload.Status.Should().Be(AllocationStatus.FullyAllocated);
        payload.Allocations.Single(allocation => allocation.Batch == "LOT-NEAR").Qty.Should().Be(5m, "batch dekat-expiry habis dulu (FEFO)");
        payload.Allocations.Single(allocation => allocation.Batch == "LOT-FAR").Qty.Should().Be(3m, "sisa dari batch jauh expiry");
    }

    [Fact]
    public async Task Two_lines_same_sku_allocate_sequentially_from_shared_stock()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 10m);
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var wave = WaveReleasedFactory.With(
            Guid.NewGuid(),
            WaveReleasedFactory.Line(orderA, qty: 6m),
            WaveReleasedFactory.Line(orderB, qty: 6m));

        var result = await PipelineRunner.ConsumeAsync(_provider, wave, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var payload = Payload((await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName))[0]);
        payload.Status.Should().Be(AllocationStatus.PartiallyAllocated);
        payload.Allocations.Where(allocation => allocation.OrderId == orderA).Sum(allocation => allocation.Qty)
            .Should().Be(6m, "order pertama penuh dari stok bersama");
        payload.Allocations.Where(allocation => allocation.OrderId == orderB).Sum(allocation => allocation.Qty)
            .Should().Be(4m, "order kedua lihat availability sudah berkurang (identity resolution EF)");
        var shortfall = payload.Shortfalls.Should().ContainSingle().Which;
        shortfall.OrderId.Should().Be(orderB);
        shortfall.ShortQty.Should().Be(2m);
        (await PipelineRunner.ReservationsAsync(_provider)).Sum(reservation => reservation.Qty)
            .Should().Be(10m, "Σ reservasi = qty stok — tak over-allocate lintas line SKU sama");
    }

    [Fact]
    public async Task Short_creates_no_phantom_reservation_and_keeps_available_non_negative()
    {
        var stockId = await StockSeeder.SeedAvailableAsync(_provider, qty: 8m);

        await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(Guid.NewGuid(), qty: 10m)), Guid.NewGuid());

        var reservations = await PipelineRunner.ReservationsAsync(_provider);
        reservations.Should().ContainSingle("hanya reservasi untuk qty tersedia (8) — no phantom untuk short (2)");
        reservations[0].Qty.Should().Be(8m);

        var stock = (await PipelineRunner.StocksAsync(_provider)).Single(s => s.Id.Value == stockId);
        stock.AvailableQty.Should().Be(0m, "Σ klaim (8) = qty (8) → availableQty tak negatif");
    }

    [Fact]
    public async Task Line_with_non_positive_qty_fails_the_wave_and_persists_nothing()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var wave = WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(Guid.NewGuid(), qty: 0m));

        var result = await PipelineRunner.ConsumeAsync(_provider, wave, Guid.NewGuid());

        result.ErrorType.Should().Be(
            ResultErrorType.Validation, "line qty ≤ 0 = event malformed, gagal bersih di trust boundary");
        (await PipelineRunner.ReservationsAsync(_provider)).Should().BeEmpty();
        (await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName)).Should().BeEmpty();
    }

    [Fact]
    public async Task Replay_same_event_id_is_no_op_via_inbox_guard()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var wave = WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(Guid.NewGuid(), qty: 10m));
        var eventId = Guid.NewGuid();

        (await PipelineRunner.ConsumeAsync(_provider, wave, eventId)).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, wave, eventId)).IsSuccess.Should().BeTrue();

        (await PipelineRunner.ReservationsAsync(_provider)).Should().ContainSingle("redelivery eventId sama = no-op");
        (await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName)).Should().ContainSingle();
    }

    [Fact]
    public async Task Replay_same_wave_different_event_id_is_no_op_via_natural_key()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var wave = WaveReleasedFactory.With(Guid.NewGuid(), WaveReleasedFactory.Line(Guid.NewGuid(), qty: 10m));

        (await PipelineRunner.ConsumeAsync(_provider, wave, Guid.NewGuid())).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, wave, Guid.NewGuid())).IsSuccess.Should().BeTrue();

        (await PipelineRunner.ReservationsAsync(_provider)).Should().ContainSingle(
            "natural key (waveId, orderId, sku) → line tak dialokasi ulang");
        (await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName)).Should().ContainSingle(
            "line ter-skip semua → tak emit event hantu");
    }

    [Fact]
    public async Task Failed_line_rolls_back_earlier_reservation_and_outbox()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var wave = WaveReleasedFactory.With(
            Guid.NewGuid(),
            WaveReleasedFactory.Line(Guid.NewGuid(), sku: "SKU-MILK", qty: 10m),
            WaveReleasedFactory.Line(Guid.NewGuid(), sku: " ", qty: 5m));

        var result = await PipelineRunner.ConsumeAsync(_provider, wave, Guid.NewGuid());

        result.IsFailure.Should().BeTrue("SKU line-2 invalid → handler gagal");
        (await PipelineRunner.ReservationsAsync(_provider)).Should().BeEmpty("abort, reservasi line-1 ikut rollback (anti dual-write)");
        (await PipelineRunner.OutboxRowsAsync(_provider, AllocationLogicalName)).Should().BeEmpty();
    }

    [Fact]
    public async Task Concurrent_allocation_of_same_stock_one_conflicts_no_over_allocate()
    {
        var stockId = await StockSeeder.SeedAvailableAsync(_provider, qty: 10m);

        using var scopeA = _provider.CreateScope();
        using var scopeB = _provider.CreateScope();

        // Kedua scope memuat Stock pada versi xmin sama sebelum salah satu commit
        var commitA = await StageFullReservationAsync(scopeA, Guid.NewGuid());
        var commitB = await StageFullReservationAsync(scopeB, Guid.NewGuid());

        var first = await commitA();
        var second = await commitB();

        first.IsSuccess.Should().BeTrue("pemenang commit lebih dulu");
        second.ErrorType.Should().Be(ResultErrorType.Conflict, "kalah xmin, 409");

        (await PipelineRunner.ReservationsAsync(_provider)).Should().ContainSingle("tak over-allocate: hanya satu reservasi persist");
        var stock = (await PipelineRunner.StocksAsync(_provider)).Single(s => s.Id.Value == stockId);
        stock.AvailableQty.Should().Be(0m);
    }

    // load Stock dan siapkan reservasi penuh (root + klaim) di scopenya
    private static async Task<Func<Task<Result>>> StageFullReservationAsync(IServiceScope scope, Guid waveId)
    {
        var stockRepository = scope.ServiceProvider.GetRequiredService<IStockRepository>();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IStockReservationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var stock = (await stockRepository.GetAllocatableAsync(Sku.Create("SKU-MILK").Value)).Single();
        var reservationId = StockReservationId.Create(Guid.NewGuid()).Value;
        var reservation = StockReservation.Create(
            reservationId, stock.Id, waveId, Guid.NewGuid(), stock.Sku, stock.Batch, Quantity.Create(stock.Qty).Value).Value;
        await reservationRepository.AddAsync(reservation);
        stock.Reserve(reservationId, reservation.WaveId, reservation.OrderId, Quantity.Create(stock.Qty).Value);
        stock.ClearDomainEvents();

        return () => unitOfWork.SaveChangesAsync();
    }

    private static StockAllocationCompleted Payload(OutboxRecord row) =>
        JsonSerializer.Deserialize<StockAllocationCompleted>(row.Payload, MessageEnvelope.PayloadSerializerOptions)!;
}

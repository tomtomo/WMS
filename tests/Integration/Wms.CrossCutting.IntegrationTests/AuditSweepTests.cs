using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.CrossCutting.IntegrationTests.TestSupport;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inventory.Application.Features.DetectNearExpiry;
using Wms.Inventory.Application.Features.ReceiveGoodsReceipt;
using Wms.Inventory.Infrastructure;
using Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;
using Wms.MasterData.Application.Features.Warehouse.UpdateWarehouse;
using Wms.MasterData.Infrastructure;
using Xunit;

namespace Wms.CrossCutting.IntegrationTests;

// Memastikan setiap command yang mengubah data menghasilkan audit log.
[Collection(CrossCuttingCollection.Name)]
public sealed class AuditSweepTests(CrossCuttingFixture fixture)
{
    [Fact]
    public async Task Inbound_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildInboundAsync(await fixture.CreateFreshDatabaseAsync("audit_inbound"));

        var created = await ModuleHosts.SendAsync(
            host,
            new Wms.Inbound.Application.Features.CreateGoodsReceiptHeader.CreateGoodsReceiptHeaderCommand(
                "PO-CC-1",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DOCK-1",
                [new Wms.Inbound.Application.Features.CreateGoodsReceiptHeader.ExpectedLineInput("SKU-A", 10m, "EA")]));

        created.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Actor.Should().Be(FixedCurrentUser.UserIdValue);
        rows[0].Action.Should().Be("CreateGoodsReceiptHeaderCommand");
    }

    [Fact]
    public async Task Inventory_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildInventoryAsync(await fixture.CreateFreshDatabaseAsync("audit_inventory"));

        var scanned = await ModuleHosts.SendAsync(host, new DetectNearExpiryCommand(30));

        scanned.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Action.Should().Be(nameof(DetectNearExpiryCommand));
    }

    [Fact]
    public async Task Outbound_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildOutboundAsync(await fixture.CreateFreshDatabaseAsync("audit_outbound"));
        var orderId = await OutboundSeeder.SeedNewOrderAsync(host, "SKU-A", 5m);

        var released = await ModuleHosts.SendAsync(
            host,
            new Wms.Outbound.Application.Features.CreateWave.CreateWaveCommand([orderId], Guid.NewGuid()));

        released.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Action.Should().Be("CreateWaveCommand");
    }

    [Fact]
    public async Task MasterData_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildMasterDataAsync(await fixture.CreateFreshDatabaseAsync("audit_masterdata"));

        var created = await ModuleHosts.SendAsync(host, new CreateWarehouseCommand("Gudang Pusat", "Jl. Merdeka 1"));

        created.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Actor.Should().Be(FixedCurrentUser.UserIdValue);
        rows[0].Action.Should().Be(nameof(CreateWarehouseCommand));
    }

    [Fact]
    public async Task Auth_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildAuthAsync(await fixture.CreateFreshDatabaseAsync("audit_auth"));

        var created = await ModuleHosts.SendAsync(
            host,
            new Wms.Auth.Application.Features.CreateRole.CreateRoleCommand("wave-lead", "Wave Lead", []));

        created.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Action.Should().Be("CreateRoleCommand");
    }

    [Fact]
    public async Task Notifications_command_mutating_menulis_satu_baris_audit()
    {
        await using var host = await ModuleHosts.BuildNotificationsAsync(await fixture.CreateFreshDatabaseAsync("audit_notifications"));
        var deliveryId = await NotificationDeliverySeeder.SeedInAppDeliveryAsync(host, Guid.NewGuid());

        var read = await ModuleHosts.SendAsync(
            host,
            new Wms.Notifications.Deliveries.MarkDeliveryRead.MarkDeliveryReadCommand(deliveryId));

        read.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Action.Should().Be("MarkDeliveryReadCommand");
    }

    [Fact]
    public async Task Commit_gagal_state_batal_tapi_baris_audit_tetap_ada()
    {
        await using var host = await ModuleHosts.BuildMasterDataAsync(await fixture.CreateFreshDatabaseAsync("audit_rollback"));
        var created = await ModuleHosts.SendAsync(host, new CreateWarehouseCommand("Gudang Pusat", "Jl. Merdeka 1"));
        var warehouseId = created.Value;

        // Paksa conflict dengan update dari koneksi lain setelah handler sukses.
        host.GetRequiredService<ConflictInjector>().ArmOnce(async () =>
        {
            using var scope = host.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
            await context.Set<Wms.MasterData.Domain.Warehouse>()
                .Where(warehouse => warehouse.Id == Wms.MasterData.Domain.WarehouseId.Create(warehouseId).Value)
                .ExecuteUpdateAsync(setters => setters.SetProperty(warehouse => warehouse.Name, warehouse => warehouse.Name));
        });

        var updated = await ModuleHosts.SendAsync(host, new UpdateWarehouseCommand(warehouseId, "Gudang Baru", "Jl. Baru 2"));

        updated.IsFailure.Should().BeTrue();
        updated.ErrorType.Should().Be(ResultErrorType.Conflict);
        updated.Error.Code.Should().Be("concurrency.conflict");

        // State tidak berubah — commit batal utuh.
        var name = await ModuleHosts.QueryAsync<MasterDataDbContext, string>(host, async context =>
            (await context.Set<Wms.MasterData.Domain.Warehouse>().AsNoTracking()
                .SingleAsync(warehouse => warehouse.Id == Wms.MasterData.Domain.WarehouseId.Create(warehouseId).Value)).Name);
        name.Should().Be("Gudang Pusat");

        // Audit log tetap tersimpan meski commit command gagal.
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().HaveCount(2);
        rows[1].Action.Should().Be(nameof(UpdateWarehouseCommand));
    }

    [Fact]
    public async Task Consumer_menulis_state_dengan_actor_SYSTEM_tanpa_baris_audit_log()
    {
        await using var host = await ModuleHosts.BuildInventoryAsync(
            await fixture.CreateFreshDatabaseAsync("audit_consumer"),
            services => services.AddSystemCurrentUser());

        using (var scope = host.CreateScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<GRConfirmedConsumer>();
            var consumed = await consumer.ConsumeAsync(
                new GRConfirmed(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    [new ReceivedLine("SKU-A", 10m, "B-01", new DateOnly(2027, 12, 31), ReceivedLineStatus.Good)],
                    []),
                Guid.NewGuid());
            consumed.IsSuccess.Should().BeTrue();
        }

        var stocks = await ModuleHosts.QueryAsync<InventoryDbContext, List<Wms.Inventory.Domain.Stock>>(
            host,
            context => context.Set<Wms.Inventory.Domain.Stock>().AsNoTracking().ToListAsync());
        stocks.Should().NotBeEmpty();
        stocks.Should().OnlyContain(stock => stock.CreatedBy == ICurrentUser.SystemActor);

        // Consumer tidak menulis ke audit_log,audit_log hanya untuk command lewat write pipeline.
        (await ModuleHosts.AuditLogRowsAsync(host)).Should().BeEmpty();
    }

    [Fact]
    public async Task Background_command_via_pipeline_ber_actor_SYSTEM()
    {
        await using var host = await ModuleHosts.BuildInventoryAsync(
            await fixture.CreateFreshDatabaseAsync("audit_background"),
            services => services.AddSystemCurrentUser());

        var scanned = await ModuleHosts.SendAsync(host, new DetectNearExpiryCommand(30));

        scanned.IsSuccess.Should().BeTrue();
        var rows = await ModuleHosts.AuditLogRowsAsync(host);
        rows.Should().ContainSingle();
        rows[0].Actor.Should().Be(ICurrentUser.SystemActor);
    }
}

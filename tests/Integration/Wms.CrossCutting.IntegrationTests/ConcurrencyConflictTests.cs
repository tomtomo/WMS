using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.CrossCutting.IntegrationTests.TestSupport;
using Wms.MasterData.Infrastructure;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Outbound.Domain;
using Wms.Outbound.Infrastructure;
using Xunit;

namespace Wms.CrossCutting.IntegrationTests;

// Memastikan update stale menghasilkan Conflict tanpa menimpa data terbaru.
[Collection(CrossCuttingCollection.Name)]
public sealed class ConcurrencyConflictTests(CrossCuttingFixture fixture)
{
    [Fact]
    public async Task Outbound_order_stale_kena_conflict_tanpa_lost_update()
    {
        await using var host = await ModuleHosts.BuildOutboundAsync(await fixture.CreateFreshDatabaseAsync("xmin_outbound"));
        var orderId = await OutboundSeeder.SeedNewOrderAsync(host, "SKU-A", 5m);
        var winnerWave = WaveId.Create(Guid.NewGuid()).Value;
        var staleWave = WaveId.Create(Guid.NewGuid()).Value;

        using var staleScope = host.CreateScope();
        var staleOrder = await staleScope.ServiceProvider.GetRequiredService<OutboundDbContext>()
            .Set<OutboundOrder>().SingleAsync(order => order.Id == OutboundOrderId.Create(orderId).Value);

        using (var winnerScope = host.CreateScope())
        {
            var context = winnerScope.ServiceProvider.GetRequiredService<OutboundDbContext>();
            var order = await context.Set<OutboundOrder>()
                .SingleAsync(candidate => candidate.Id == OutboundOrderId.Create(orderId).Value);
            order.AssignToWave(winnerWave).IsSuccess.Should().BeTrue();
            order.ClearDomainEvents();
            await context.SaveChangesAsync();
        }

        // Snapshot stale masih lolos rule domain, conflict baru muncul saat commit.
        staleOrder.AssignToWave(staleWave).IsSuccess.Should().BeTrue();
        staleOrder.ClearDomainEvents();
        var conflicted = await staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        conflicted.IsFailure.Should().BeTrue();
        conflicted.ErrorType.Should().Be(ResultErrorType.Conflict);
        conflicted.Error.Code.Should().Be("concurrency.conflict");

        var persistedWave = await ModuleHosts.QueryAsync<OutboundDbContext, Guid?>(host, async context =>
            (await context.Set<OutboundOrder>().AsNoTracking()
                .SingleAsync(order => order.Id == OutboundOrderId.Create(orderId).Value)).WaveId?.Value);
        persistedWave.Should().Be(winnerWave.Value, "pemenang commit pertama tidak boleh tertimpa (no lost update)");
    }

    [Fact]
    public async Task Auth_role_stale_kena_conflict_tanpa_lost_update()
    {
        await using var host = await ModuleHosts.BuildAuthAsync(await fixture.CreateFreshDatabaseAsync("xmin_auth"));
        var created = await ModuleHosts.SendAsync(
            host,
            new Wms.Auth.Application.Features.CreateRole.CreateRoleCommand("wave-lead", "Wave Lead", []));
        var roleId = Wms.Auth.Domain.RoleId.Create(created.Value).Value;

        using var staleScope = host.CreateScope();
        var staleRole = await staleScope.ServiceProvider.GetRequiredService<Wms.Auth.Infrastructure.AuthDbContext>()
            .Set<Wms.Auth.Domain.Role>().SingleAsync(role => role.Id == roleId);

        using (var winnerScope = host.CreateScope())
        {
            var context = winnerScope.ServiceProvider.GetRequiredService<Wms.Auth.Infrastructure.AuthDbContext>();
            var role = await context.Set<Wms.Auth.Domain.Role>().SingleAsync(candidate => candidate.Id == roleId);
            role.Rename("Wave Lead Senior").IsSuccess.Should().BeTrue();
            await context.SaveChangesAsync();
        }

        staleRole.Rename("Wave Lead Stale").IsSuccess.Should().BeTrue();
        var conflicted = await staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        conflicted.ErrorType.Should().Be(ResultErrorType.Conflict);

        var persistedName = await ModuleHosts.QueryAsync<Wms.Auth.Infrastructure.AuthDbContext, string>(host, async context =>
            (await context.Set<Wms.Auth.Domain.Role>().AsNoTracking().SingleAsync(role => role.Id == roleId)).Name);
        persistedName.Should().Be("Wave Lead Senior");
    }

    [Fact]
    public async Task Notifications_delivery_stale_kena_conflict_tanpa_lost_update()
    {
        await using var host = await ModuleHosts.BuildNotificationsAsync(await fixture.CreateFreshDatabaseAsync("xmin_notifications"));
        var deliveryId = await NotificationDeliverySeeder.SeedInAppDeliveryAsync(host, Guid.NewGuid());
        var typedId = DeliveryId.Create(deliveryId).Value;

        using var staleScope = host.CreateScope();
        var staleDelivery = await staleScope.ServiceProvider.GetRequiredService<NotificationsDbContext>()
            .Set<NotificationDelivery>().SingleAsync(delivery => delivery.Id == typedId);

        using (var winnerScope = host.CreateScope())
        {
            var context = winnerScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var delivery = await context.Set<NotificationDelivery>().SingleAsync(candidate => candidate.Id == typedId);
            delivery.MarkSent("provider-msg-1").IsSuccess.Should().BeTrue();
            await context.SaveChangesAsync();
        }

        // Snapshot stale masih lolos rule domain, conflict baru muncul saat commit.
        staleDelivery.MarkSent("provider-msg-stale").IsSuccess.Should().BeTrue();
        var conflicted = await staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        conflicted.ErrorType.Should().Be(ResultErrorType.Conflict);
        conflicted.Error.Code.Should().Be("concurrency.conflict");

        var persistedProviderId = await ModuleHosts.QueryAsync<NotificationsDbContext, string?>(host, async context =>
            (await context.Set<NotificationDelivery>().AsNoTracking()
                .SingleAsync(delivery => delivery.Id == typedId)).ProviderMessageId);
        persistedProviderId.Should().Be("provider-msg-1");
    }

    [Fact]
    public void Aggregate_modul_collapsed_ber_xmin_token()
    {
        // Notifications di luar assembly graph Architecture.Tests
        var options = new DbContextOptionsBuilder<NotificationsDbContext>().UseNpgsql().Options;
        using var context = new NotificationsDbContext(options);

        var aggregates = context.Model.GetEntityTypes()
            .Where(entity => IsAggregateRoot(entity.ClrType))
            .ToList();

        aggregates.Should().HaveCount(2, "NotificationSubscription + NotificationDelivery");
        aggregates.Should().OnlyContain(
            entity => entity.FindProperty("xmin") != null && entity.FindProperty("xmin")!.IsConcurrencyToken,
            "tiap aggregate root wajib UseXminAsConcurrencyToken");
    }

    [Fact]
    public async Task Update_konkuren_lewat_REST_menjadi_409_problem()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync("xmin_rest");
        var app = await MasterDataApiHost.StartAsync(connectionString);
        await using (app.ConfigureAwait(false))
        {
            var client = app.GetTestClient();
            var created = await client.PostAsJsonAsync("/v1/warehouses", new { name = "Gudang Pusat", address = "Jl. Merdeka 1" });
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var warehouseId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("warehouseId").GetGuid();

            // Paksa conflict dengan update dari koneksi lain setelah handler sukses.
            app.Services.GetRequiredService<ConflictInjector>().ArmOnce(async () =>
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
                await context.Set<Wms.MasterData.Domain.Warehouse>()
                    .Where(warehouse => warehouse.Id == Wms.MasterData.Domain.WarehouseId.Create(warehouseId).Value)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(warehouse => warehouse.Name, warehouse => warehouse.Name));
            });

            var response = await client.PutAsJsonAsync(
                $"/v1/warehouses/{warehouseId}",
                new { name = "Gudang Baru", address = "Jl. Baru 2" });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict, "konflik xmin di-surface sekali ke caller, tanpa auto-retry");
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            problem.GetProperty("errorCode").GetString().Should().Be("concurrency.conflict");

            // Response 409 tetap membawa correlationId untuk tracing.
            problem.TryGetProperty("correlationId", out _).Should().BeTrue();
        }
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(Wms.BuildingBlocks.Domain.Primitives.AggregateRoot<>))
            {
                return true;
            }
        }

        return false;
    }
}

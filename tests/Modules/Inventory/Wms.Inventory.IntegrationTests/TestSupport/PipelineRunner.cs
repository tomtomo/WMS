using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Inbound.Contracts;
using Wms.Inventory.Application.Features.AllocateWave;
using Wms.Inventory.Application.Features.FulfillReservation;
using Wms.Inventory.Application.Features.ReceiveGoodsReceipt;
using Wms.Inventory.Application.Features.RemovePickedStock;
using Wms.Inventory.Domain;
using Wms.Inventory.Infrastructure;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Satu scope DI per operasi = satu request.
internal static class PipelineRunner
{
    public static async Task<TResponse> SendAsync<TResponse>(IServiceProvider provider, IRequest<TResponse> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    // Consumer receiving in process
    public static async Task<Result> ConsumeAsync(IServiceProvider provider, GRConfirmed integrationEvent, Guid eventId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<GRConfirmedConsumer>()
            .ConsumeAsync(integrationEvent, eventId);
    }

    // Consumer alokasi in process
    public static async Task<Result> ConsumeAsync(IServiceProvider provider, WaveReleased integrationEvent, Guid eventId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<WaveReleasedConsumer>()
            .ConsumeAsync(integrationEvent, eventId);
    }

    // Consumer picking in process
    public static async Task<Result> ConsumeAsync(IServiceProvider provider, PickingCompleted integrationEvent, Guid eventId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PickingCompletedConsumer>()
            .ConsumeAsync(integrationEvent, eventId);
    }

    // Consumer dispatch in process
    public static async Task<Result> ConsumeAsync(IServiceProvider provider, ShipmentDispatched integrationEvent, Guid eventId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ShipmentDispatchedConsumer>()
            .ConsumeAsync(integrationEvent, eventId);
    }

    public static Task<List<StockReservation>> ReservationsAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<StockReservation>().AsNoTracking().ToListAsync());

    public static async Task<List<OutboxRecord>> OutboxRowsAsync(IServiceProvider provider, string? logicalName = null)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var query = context.Set<OutboxRecord>().AsNoTracking();
        if (logicalName is not null)
        {
            query = query.Where(record => record.LogicalName == logicalName);
        }

        return await query.OrderBy(record => record.OccurredAt).ToListAsync();
    }

    public static async Task<T> QueryDbAsync<T>(IServiceProvider provider, Func<InventoryDbContext, Task<T>> query)
    {
        using var scope = provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<InventoryDbContext>());
    }

    public static Task<List<Stock>> StocksAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<Stock>().AsNoTracking().ToListAsync());

    public static Task<List<PutawayTask>> TasksAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<PutawayTask>().AsNoTracking().ToListAsync());
}

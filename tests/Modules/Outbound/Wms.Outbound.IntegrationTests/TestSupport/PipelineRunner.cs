using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Inventory.Contracts;
using Wms.Outbound.Application.Features.HandleStockAllocationCompleted;
using Wms.Outbound.Domain;
using Wms.Outbound.Infrastructure;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Satu scope DI per operasi = satu request.
internal static class PipelineRunner
{
    public static async Task<TResponse> SendAsync<TResponse>(IServiceProvider provider, IRequest<TResponse> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    // Consumer alokasi in process.
    public static async Task<Result> ConsumeAsync(IServiceProvider provider, StockAllocationCompleted integrationEvent, Guid eventId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<StockAllocationCompletedConsumer>()
            .ConsumeAsync(integrationEvent, eventId);
    }

    public static async Task<List<OutboxRecord>> OutboxRowsAsync(IServiceProvider provider, string? logicalName = null)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboundDbContext>();
        var query = context.Set<OutboxRecord>().AsNoTracking();
        if (logicalName is not null)
        {
            query = query.Where(record => record.LogicalName == logicalName);
        }

        return await query.OrderBy(record => record.OccurredAt).ToListAsync();
    }

    public static async Task<T> QueryDbAsync<T>(IServiceProvider provider, Func<OutboundDbContext, Task<T>> query)
    {
        using var scope = provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<OutboundDbContext>());
    }

    public static Task<List<Wave>> WavesAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<Wave>().AsNoTracking().ToListAsync());

    public static Task<List<OutboundOrder>> OrdersAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<OutboundOrder>().AsNoTracking().ToListAsync());

    public static Task<List<PickingTask>> PickingTasksAsync(IServiceProvider provider) =>
        QueryDbAsync(provider, context => context.Set<PickingTask>().AsNoTracking().ToListAsync());

    public static T Payload<T>(OutboxRecord row) =>
        JsonSerializer.Deserialize<T>(row.Payload, MessageEnvelope.PayloadSerializerOptions)!;
}

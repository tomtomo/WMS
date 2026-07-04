using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Inbound.Infrastructure;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Satu scope DI per Send = satu request
internal static class PipelineRunner
{
    public static async Task<TResponse> SendAsync<TResponse>(IServiceProvider provider, IRequest<TResponse> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public static async Task<List<OutboxRecord>> OutboxRowsAsync(IServiceProvider provider, string? logicalName = null)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InboundDbContext>();
        var query = context.Set<OutboxRecord>().AsNoTracking();
        if (logicalName is not null)
        {
            query = query.Where(record => record.LogicalName == logicalName);
        }

        return await query.OrderBy(record => record.OccurredAt).ToListAsync();
    }

    public static async Task<T> QueryDbAsync<T>(IServiceProvider provider, Func<InboundDbContext, Task<T>> query)
    {
        using var scope = provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<InboundDbContext>());
    }
}

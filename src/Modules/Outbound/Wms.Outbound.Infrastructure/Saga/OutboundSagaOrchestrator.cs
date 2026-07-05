using System.Text.Json;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Outbound.Infrastructure.Saga;

// Orchestrator saga in proc milik Outbound — state persist di DB Outbound (schema outbound), bukan koordinator
// global. Cloud: Durable Functions / Workflows.
internal sealed class OutboundSagaOrchestrator(OutboundDbContext context, TimeProvider timeProvider) : ISagaOrchestrator
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task StartAsync<TSagaData>(string sagaId, TSagaData data, CancellationToken cancellationToken = default)
        where TSagaData : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);
        ArgumentNullException.ThrowIfNull(data);

        var state = JsonSerializer.Serialize(data, _serializerOptions);
        var saga = SagaState.Start(sagaId, typeof(TSagaData).Name, state, timeProvider.GetUtcNow());

        context.Set<SagaState>().Add(saga);
        await context.SaveChangesAsync(cancellationToken);
    }
}

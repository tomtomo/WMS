namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Event analitik (BigQuery GCP, Synapse atau Fabric Azure, stub CSV Local).
public interface IAnalyticsSink
{
    Task EmitAsync<TEvent>(TEvent analyticsEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}

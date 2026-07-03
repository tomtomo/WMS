namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Saga multi step berkompensasi milik Outbound: in-proc state machine Local, Durable Functions Azure, Workflows GCP.
public interface ISagaOrchestrator
{
    Task StartAsync<TSagaData>(string sagaId, TSagaData data, CancellationToken cancellationToken = default)
        where TSagaData : notnull;
}

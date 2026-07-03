namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Marker pasangan (eventId, handler) sudah diproses sehingga pengiriman ganda broker at least once menjadi no op.
public interface IInboxGuard
{
    Task<bool> HasProcessedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default);
}

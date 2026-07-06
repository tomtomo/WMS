using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Publisher untuk mengirim message envelope.
public interface IMessagePublisher
{
    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
}

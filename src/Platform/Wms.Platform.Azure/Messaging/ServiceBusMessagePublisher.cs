using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Publisher untuk rail core flow lewat topic Service Bus. Urutan event dijaga per session dari partition key envelope.
// Bukan implement IMessagePublisher
public sealed class ServiceBusMessagePublisher : IAsyncDisposable
{
    private readonly Lazy<ServiceBusSender> _sender;

    public ServiceBusMessagePublisher(ServiceBusClient client, IOptions<AzureMessagingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        // Sender dibuat sekali dan dipakai ulang. Lazy dipakai supaya constructor tidak langsung menyentuh jaringan.
        _sender = new Lazy<ServiceBusSender>(() => client.CreateSender(options.Value.CoreFlowTopicName));
    }

    public async Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.DeliveryClass != DeliveryClass.CoreFlow)
        {
            throw new InvalidOperationException(
                $"Salah rail: '{envelope.LogicalName}' ({envelope.DeliveryClass}) bukan CoreFlow — notification lewat Event Grid.");
        }

        await _sender.Value
            .SendMessageAsync(ServiceBusEnvelopeMapper.ToServiceBusMessage(envelope), cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender.IsValueCreated)
        {
            await _sender.Value.DisposeAsync().ConfigureAwait(false);
        }
    }
}

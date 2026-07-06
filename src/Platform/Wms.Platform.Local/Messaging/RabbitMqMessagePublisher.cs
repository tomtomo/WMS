using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Platform.Local.Messaging;

// Publisher broker Local (cloud: Service Bus core / Event Grid notif).
public sealed class RabbitMqMessagePublisher(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        using var channel = connectionFactory.CreateChannel();
        channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);

        // Pastikan broker menerima message sebelum dianggap berhasil publish.
        channel.ConfirmSelect();

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = envelope.EventId.ToString();
        properties.Type = envelope.LogicalName;
        properties.ContentType = "application/json";
        properties.Headers = new Dictionary<string, object>
        {
            ["delivery-class"] = envelope.DeliveryClass.ToString(),
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        channel.BasicPublish(
            exchange: settings.ExchangeName,
            routingKey: RabbitMqRouting.RoutingKey(envelope.DeliveryClass, envelope.LogicalName),
            mandatory: false,
            basicProperties: properties,
            body: body);
        channel.WaitForConfirmsOrDie(settings.PublisherConfirmTimeout);

        return Task.CompletedTask;
    }
}

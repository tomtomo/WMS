using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Messaging;

namespace Wms.Platform.Local.Messaging;

// Subscriber broker Local: queue durable per (service, event)
public sealed class RabbitMqMessageSubscriber(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessageSubscriber> logger) : IMessageSubscriber, IDisposable
{
    private readonly ConcurrentBag<IModel> _channels = [];

    public Task SubscribeAsync<TIntegrationEvent>(
        Func<TIntegrationEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var logicalName = IntegrationEventLogicalName.Resolve(typeof(TIntegrationEvent));
        var queueName = $"{settings.SubscriberQueuePrefix}.{logicalName}";

        var channel = connectionFactory.CreateChannel();
        _channels.Add(channel);

        channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(queueName, settings.ExchangeName, routingKey: logicalName);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, delivery) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(delivery.Body.Span);
                var envelope = JsonSerializer.Deserialize<MessageEnvelope>(json);
                var integrationEvent = envelope is null
                    ? default
                    : JsonSerializer.Deserialize<TIntegrationEvent>(envelope.Payload);
                if (integrationEvent is null)
                {
                    throw new JsonException($"Envelope atau payload '{logicalName}' tidak bisa dibaca.");
                }

                await handler(integrationEvent, cancellationToken).ConfigureAwait(false);
                Acknowledge(channel, delivery.DeliveryTag, processed: true);
            }
#pragma warning disable S2221 // Boundary konsumsi broker: kegagalan apa pun berujung nack no requeue, bukan crash consumer loop.
            catch (Exception exception)
#pragma warning restore S2221
            {
                logger.LogWarning(
                    exception,
                    "Pesan {LogicalName} gagal diproses; nack tanpa requeue (deliveryTag {DeliveryTag})",
                    logicalName,
                    delivery.DeliveryTag);
                Acknowledge(channel, delivery.DeliveryTag, processed: false);
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }
    }

    // Race shutdown: Dispose bisa menutup channel saat handler in flight
    private void Acknowledge(IModel channel, ulong deliveryTag, bool processed)
    {
        try
        {
            if (processed)
            {
                channel.BasicAck(deliveryTag, multiple: false);
            }
            else
            {
                channel.BasicNack(deliveryTag, multiple: false, requeue: false);
            }
        }
        catch (Exception exception) when (exception is ObjectDisposedException or AlreadyClosedException)
        {
            logger.LogDebug(exception, "Ack dilewati: channel tertutup saat shutdown (deliveryTag {DeliveryTag})", deliveryTag);
        }
    }
}

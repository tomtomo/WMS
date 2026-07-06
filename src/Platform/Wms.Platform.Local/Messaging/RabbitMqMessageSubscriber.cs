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

namespace Wms.Platform.Local.Messaging;

// Subscriber broker Local, IMessageSubscriber berbasis RabbitMQ.
public sealed class RabbitMqMessageSubscriber(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessageSubscriber> logger) : IMessageSubscriber, IDisposable
{
    private readonly ConcurrentBag<IModel> _channels = [];

    public Task SubscribeAsync(
        string queueName,
        IReadOnlyCollection<RailSubscription> subscriptions,
        Func<MessageEnvelope, CancellationToken, Task<bool>> onMessageAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(onMessageAsync);
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var channel = connectionFactory.CreateChannel();
        _channels.Add(channel);

        channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        foreach (var subscription in subscriptions)
        {
            // Bind queue berdasarkan delivery class dan logical name yang disubscribe.
            channel.QueueBind(
                queueName,
                settings.ExchangeName,
                RabbitMqRouting.RoutingKey(subscription.DeliveryClass, subscription.LogicalName));
        }

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, delivery) =>
        {
            var handled = false;
            try
            {
                var json = Encoding.UTF8.GetString(delivery.Body.Span);
                var envelope = JsonSerializer.Deserialize<MessageEnvelope>(json)
                    ?? throw new JsonException($"Envelope di queue '{queueName}' tidak bisa dibaca.");
                handled = await onMessageAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable S2221
            catch (Exception exception)
#pragma warning restore S2221
            {
                logger.LogWarning(
                    exception,
                    "Pesan di queue {Queue} gagal diproses; nack tanpa requeue (deliveryTag {DeliveryTag})",
                    queueName,
                    delivery.DeliveryTag);
            }

            Acknowledge(channel, delivery.DeliveryTag, handled);
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

    // Channel bisa tertutup saat proses shutdown.
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

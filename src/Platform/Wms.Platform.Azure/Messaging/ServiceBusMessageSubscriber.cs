using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Subscriber untuk rail core flow lewat Service Bus. Message diproses berurutan per session dan diselesaikan manual.
public sealed class ServiceBusMessageSubscriber(
    ServiceBusClient client,
    ServiceBusAdministrationClient administrationClient,
    IOptions<AzureMessagingOptions> options,
    ILogger<ServiceBusMessageSubscriber> logger) : IMessageSubscriber, IAsyncDisposable
{
    private readonly ConcurrentBag<ServiceBusSessionProcessor> _processors = [];

    public async Task SubscribeAsync(
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
        var coreFlowNames = subscriptions
            .Where(subscription => subscription.DeliveryClass == DeliveryClass.CoreFlow)
            .Select(subscription => subscription.LogicalName)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var skipped = subscriptions.Count(subscription => subscription.DeliveryClass == DeliveryClass.Notification);
        if (skipped > 0)
        {
            logger.LogInformation(
                "Service Bus rail: {Skipped} subscription Notification dilewati; rail notification dikonsumsi dari Event Grid.",
                skipped);
        }

        if (coreFlowNames.Count == 0)
        {
            logger.LogInformation("Service Bus rail: '{Subscription}' tak punya subscription CoreFlow; subscriber idle.", queueName);
            return;
        }

        await ServiceBusRailTopology
            .EnsureSubscriptionAsync(administrationClient, settings.CoreFlowTopicName, queueName, coreFlowNames, cancellationToken)
            .ConfigureAwait(false);

        // Satu pemrosesan aktif per session menjaga urutan event. Session yang berbeda tetap bisa berjalan paralel.
        var processor = client.CreateSessionProcessor(settings.CoreFlowTopicName, queueName, new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentSessions = 8,
            MaxConcurrentCallsPerSession = 1,
        });

        processor.ProcessMessageAsync += args => OnMessageAsync(args, onMessageAsync, queueName);
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processor error di '{Subscription}' ({Source})", queueName, args.ErrorSource);
            return Task.CompletedTask;
        };

        _processors.Add(processor);
        await processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors)
        {
            await processor.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task OnMessageAsync(
        ProcessSessionMessageEventArgs args,
        Func<MessageEnvelope, CancellationToken, Task<bool>> onMessageAsync,
        string queueName)
    {
        var handled = false;
        try
        {
            var envelope = ServiceBusEnvelopeMapper.ToEnvelope(args.Message);
            handled = await onMessageAsync(envelope, args.CancellationToken).ConfigureAwait(false);
        }
#pragma warning disable S2221 // Pesan yang gagal diproses tidak boleh mematikan processor. Biarkan retry sampai masuk DLQ.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore S2221
        {
            logger.LogWarning(
                exception,
                "Pesan di '{Subscription}' gagal diproses, akan dicoba lagi sampai masuk DLQ (delivery {DeliveryCount}/{Max}).",
                queueName,
                args.Message.DeliveryCount,
                ServiceBusRailTopology.MaxDeliveryCount);
        }

        if (handled)
        {
            await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken).ConfigureAwait(false);
        }
    }
}

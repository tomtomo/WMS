using System.Globalization;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.Messaging;

namespace Wms.Platform.Azure.Scheduling;

// Delay sekali jalan memakai jadwal message Service Bus, dengan queue yang sama dari konfigurasi Azure messaging.
public sealed class ServiceBusScheduledDelayedTaskQueue : IDelayedTaskQueue, IAsyncDisposable
{
    private readonly Lazy<ServiceBusSender> _sender;

    public ServiceBusScheduledDelayedTaskQueue(ServiceBusClient client, IOptions<AzureMessagingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _sender = new Lazy<ServiceBusSender>(() => client.CreateSender(options.Value.DelayedTaskQueueName));
    }

    public async Task<string> ScheduleAsync<TPayload>(
        TPayload payload,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
        where TPayload : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(payload, payload.GetType()))
        {
            ContentType = "application/json",
            Subject = typeof(TPayload).Name,
        };

        // Dipakai worker untuk memilih IDelayedTaskHandler<TPayload> saat message dibaca.
        message.ApplicationProperties["payloadType"] = typeof(TPayload).FullName;

        var sequenceNumber = await _sender.Value
            .ScheduleMessageAsync(message, dueAt, cancellationToken)
            .ConfigureAwait(false);

        return sequenceNumber.ToString(CultureInfo.InvariantCulture);
    }

    public async Task CancelAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var sequenceNumber = long.Parse(taskId, CultureInfo.InvariantCulture);
        try
        {
            await _sender.Value.CancelScheduledMessageAsync(sequenceNumber, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessageNotFound)
        {
            // Cancel dibuat aman diulang: task yang sudah jalan atau sudah dihapus dianggap selesai.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender.IsValueCreated)
        {
            await _sender.Value.DisposeAsync().ConfigureAwait(false);
        }
    }
}

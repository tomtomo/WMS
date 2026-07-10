using AwesomeAssertions;
using Azure;
using Azure.Messaging.EventGrid;
using Azure.Storage.Queues;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Event Grid belum punya emulator, jadi alur penuh ditest live lewat custom topic dan subscription ke Storage Queue.
// Kalau kredensial tidak tersedia, test dilewati dengan pesan jelas.
public sealed class EventGridLiveTests
{
    private static readonly string? _endpoint = Environment.GetEnvironmentVariable("WMS_PARITY_EG_ENDPOINT");
    private static readonly string? _key = Environment.GetEnvironmentVariable("WMS_PARITY_EG_KEY");
    private static readonly string? _queueConnection = Environment.GetEnvironmentVariable("WMS_PARITY_EG_QUEUE_CONN");
    private static readonly string? _queueName = Environment.GetEnvironmentVariable("WMS_PARITY_EG_QUEUE_NAME");

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Notification_envelope_fans_out_through_a_live_custom_topic_to_its_subscriber()
    {
        Skip.If(
            string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_key)
            || string.IsNullOrWhiteSpace(_queueConnection) || string.IsNullOrWhiteSpace(_queueName),
            "Kredensial live Event Grid tidak di-set (WMS_PARITY_EG_*) — jalankan via smoke live rg test-only.");

        var publisher = new EventGridNotificationPublisher(
            new EventGridPublisherClient(new Uri(_endpoint!), new AzureKeyCredential(_key!)));
        var envelope = new MessageEnvelope(
            Guid.NewGuid(),
            "parity.live_notification_checked.v1",
            DeliveryClass.Notification,
            DateTimeOffset.UtcNow,
            """{"probe":true}""",
            null,
            null,
            null);

        await publisher.PublishAsync(envelope);

        var queue = new QueueClient(_queueConnection, _queueName);
        var delivered = false;
        await ParityWait.UntilAsync(
            () =>
            {
                var messages = queue.ReceiveMessages(maxMessages: 32).Value;
                foreach (var message in messages)
                {
                    var body = DecodeBody(message.Body.ToString());
                    if (body.Contains(envelope.EventId.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        body.Should().Contain("parity.live_notification_checked.v1", "type CloudEvent = LogicalName");
                        delivered = true;
                        queue.DeleteMessage(message.MessageId, message.PopReceipt);
                        return true;
                    }

                    queue.DeleteMessage(message.MessageId, message.PopReceipt);
                }

                return false;
            },
            TimeSpan.FromSeconds(90),
            "CloudEvent terkirim dari custom topic ke Storage Queue subscriber");

        delivered.Should().BeTrue();
    }

    // Event Grid mengirim body base64 ke Storage Queue.
    private static string DecodeBody(string raw)
    {
        try
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(raw));
        }
        catch (FormatException)
        {
            return raw;
        }
    }
}

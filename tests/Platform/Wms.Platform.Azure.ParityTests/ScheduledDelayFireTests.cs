using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Azure.Scheduling;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Test delayed task lewat emulator Service Bus: pesan belum terbaca sebelum due, terbaca setelah due, dan bisa dibatalkan.
[Collection(ServiceBusEmulatorCollection.Name)]
public sealed class ScheduledDelayFireTests(ServiceBusEmulatorFixture emulator) : IAsyncLifetime
{
    private const string QueueName = "wms-delayed-tasks";

    private ServiceBusClient _client = null!;
    private ServiceBusScheduledDelayedTaskQueue _queue = null!;

    public async Task InitializeAsync()
    {
        // Adapter tidak membuat queue delayed-task sendiri. di test, queue dibuat dulu seperti resource IaC di cloud.
        var administration = new ServiceBusAdministrationClient(emulator.AdministrationConnectionString);
        if (!await administration.QueueExistsAsync(QueueName))
        {
            await administration.CreateQueueAsync(QueueName);
        }

        _client = new ServiceBusClient(emulator.ConnectionString);
        _queue = new ServiceBusScheduledDelayedTaskQueue(_client, Options.Create(new AzureMessagingOptions()));
    }

    public async Task DisposeAsync()
    {
        await _queue.DisposeAsync();
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task Task_fires_only_after_its_due_time()
    {
        var receiver = _client.CreateReceiver(QueueName);
        await using (receiver.ConfigureAwait(false))
        {
            await _queue.ScheduleAsync(
                new EscalatePendingGr(Guid.Parse("88888888-8888-8888-8888-888888888888")),
                DateTimeOffset.UtcNow.AddSeconds(8));

            var early = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(3));
            early.Should().BeNull("pesan terjadwal belum due");

            var fired = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));
            fired.Should().NotBeNull("pesan muncul setelah due time");
            fired!.Subject.Should().Be(nameof(EscalatePendingGr));
            fired.ApplicationProperties["payloadType"].Should().Be(typeof(EscalatePendingGr).FullName);
            fired.Body.ToString().Should().Contain("88888888-8888-8888-8888-888888888888");
            await receiver.CompleteMessageAsync(fired);
        }
    }

    [Fact]
    public async Task Cancelled_task_never_fires()
    {
        // Jadwalkan agak jauh supaya cancel tidak benturan dengan waktu aktif message di broker.
        var taskId = await _queue.ScheduleAsync(
            new EscalatePendingGr(Guid.NewGuid()),
            DateTimeOffset.UtcNow.AddSeconds(60));

        await _queue.CancelAsync(taskId);

        var receiver = _client.CreateReceiver(QueueName);
        await using (receiver.ConfigureAwait(false))
        {
            var peeked = await receiver.PeekMessageAsync(
                fromSequenceNumber: long.Parse(taskId, System.Globalization.CultureInfo.InvariantCulture));
            peeked.Should().BeNull("pesan terjadwal yang dibatalkan lenyap dari queue");
        }
    }

    private sealed record EscalatePendingGr(Guid GrId);
}

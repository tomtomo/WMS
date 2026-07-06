using AwesomeAssertions;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Notifications.Consumers;
using Wms.Notifications.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test agar delivery yang sudah terkirim tidak diproses ulang.
[Collection(PostgresCollection.Name)]
public sealed class IdempotentAlreadySentTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Replayed_event_does_not_enqueue_twice()
    {
        var operatorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var integrationEvent = SampleEvents.PutawayAssigned(operatorId, Guid.NewGuid());

        await DeliverAsync<PutawayTaskAssignedConsumer>(consumer => consumer.ConsumeAsync(integrationEvent, eventId));
        await DeliverAsync<PutawayTaskAssignedConsumer>(consumer => consumer.ConsumeAsync(integrationEvent, eventId));

        (await AllDeliveriesAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Re_dispatch_after_sent_does_not_call_port_again()
    {
        var operatorId = Guid.NewGuid();
        await DeliverAsync<PutawayTaskAssignedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.PutawayAssigned(operatorId, Guid.NewGuid()), Guid.NewGuid()));

        (await DispatchAsync()).Should().Be(2);
        InAppNotifier.ClearReceivedCalls();
        PushNotifier.ClearReceivedCalls();

        // Semua sudah Sent
        (await DispatchAsync()).Should().Be(0);
        await InAppNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await PushNotifier.DidNotReceive().PushAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

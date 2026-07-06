using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries.MarkDeliveryRead;
using Wms.Notifications.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test unread inbox untuk notifikasi in-app.
[Collection(PostgresCollection.Name)]
public sealed class InAppInboxMarkReadTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Inapp_delivery_is_unread_then_marked_read_via_command()
    {
        var operatorId = Guid.NewGuid();

        await DeliverAsync<PutawayTaskAssignedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.PutawayAssigned(operatorId, Guid.NewGuid()), Guid.NewGuid()));
        await DispatchAsync();

        var unread = await ScopedAsync(provider =>
            provider.GetRequiredService<INotificationInboxReader>().GetSummaryAsync(operatorId));
        unread.UnreadCount.Should().Be(1, "hanya InApp dihitung inbox (Push tidak)");

        var page = await ScopedAsync(provider =>
            provider.GetRequiredService<INotificationInboxReader>().ListAsync(operatorId, 1, 20));
        var item = page.Items.Should().ContainSingle().Subject;

        var marked = await ScopedAsync(provider =>
            provider.GetRequiredService<IMediator>().Send(new MarkDeliveryReadCommand(item.DeliveryId)));
        marked.IsSuccess.Should().BeTrue();

        var afterRead = await ScopedAsync(provider =>
            provider.GetRequiredService<INotificationInboxReader>().GetSummaryAsync(operatorId));
        afterRead.UnreadCount.Should().Be(0, "sudah dibaca -> hilang dari unread");
    }
}

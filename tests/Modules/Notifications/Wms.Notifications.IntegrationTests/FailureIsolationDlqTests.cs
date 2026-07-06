using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.IntegrationTests.TestSupport;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test isolasi kegagalan saat pengiriman notifikasi gagal.
[Collection(PostgresCollection.Name)]
public sealed class FailureIsolationDlqTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Provider_failure_is_dead_lettered_and_never_propagates()
    {
        var emailUser = Guid.NewGuid();
        var role = Guid.NewGuid();
        Directory.SetRoleMembers(role, emailUser);
        await SeedSubscriptionAsync(SubscriberType.Role, role, NotificationTopics.StockNearExpiry, [Channel.Email]);

        EmailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("provider down"));

        await DeliverAsync<StockNearExpiryConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.NearExpiry(Guid.NewGuid()), Guid.NewGuid()));

        // Dispatch gagal tidak boleh menghentikan alur utama.
        var dispatch = async () => await DispatchAsync();
        await dispatch.Should().NotThrowAsync();

        var deliveries = await AllDeliveriesAsync();
        var failed = deliveries.Should().ContainSingle().Subject;
        failed.State.Should().Be(DeliveryState.Failed);
        failed.RetryCount.Should().Be(ConsumerDeadLetterPipeline.MaxAttempts);

        var deadLetterCount = await QueryAsync(context => context.Set<DeadLetterRecord>().CountAsync());
        deadLetterCount.Should().Be(1, "setelah batas retry masuk DLQ");
    }

    [Fact]
    public async Task Failed_delivery_does_not_block_others_in_same_drain()
    {
        // Isolasi per delivery: satu delivery gagal tidak menghalangi delivery lain sukses.
        var emailUser = Guid.NewGuid();
        var inAppUser = Guid.NewGuid();
        var emailRole = Guid.NewGuid();
        var inAppRole = Guid.NewGuid();
        Directory.SetRoleMembers(emailRole, emailUser);
        Directory.SetRoleMembers(inAppRole, inAppUser);
        await SeedSubscriptionAsync(SubscriberType.Role, emailRole, NotificationTopics.WaveReady, [Channel.Email]);
        await SeedSubscriptionAsync(SubscriberType.Role, inAppRole, NotificationTopics.WaveReady, [Channel.InApp]);

        EmailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("provider down"));

        await DeliverAsync<WaveReadyConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.WaveReady(Guid.NewGuid()), Guid.NewGuid()));
        await DispatchAsync();

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().Contain(delivery => delivery.UserId == inAppUser && delivery.State == DeliveryState.Sent);
        deliveries.Should().Contain(delivery => delivery.UserId == emailUser && delivery.State == DeliveryState.Failed);
    }

    [Fact]
    public async Task Missing_recipient_contact_is_dead_lettered()
    {
        // Email/Push butuh detail kontak
        var user = Guid.NewGuid();
        var role = Guid.NewGuid();
        Directory.SetRoleMembers(role, user);
        Directory.SetNoRecipient(user);
        await SeedSubscriptionAsync(SubscriberType.Role, role, NotificationTopics.StockNearExpiry, [Channel.Email]);

        await DeliverAsync<StockNearExpiryConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.NearExpiry(Guid.NewGuid()), Guid.NewGuid()));

        var dispatch = async () => await DispatchAsync();
        await dispatch.Should().NotThrowAsync();

        (await AllDeliveriesAsync()).Should().ContainSingle().Which.State.Should().Be(DeliveryState.Failed);
        (await QueryAsync(context => context.Set<DeadLetterRecord>().CountAsync())).Should().Be(1);
    }
}

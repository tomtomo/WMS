using AwesomeAssertions;
using NSubstitute;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.UnitTests;

// Test SubscriptionResolver
public sealed class SubscriptionResolverTests
{
    private readonly INotificationSubscriptionReader _reader = Substitute.For<INotificationSubscriptionReader>();
    private readonly IUserDirectory _directory = Substitute.For<IUserDirectory>();
    private readonly SubscriptionResolver _resolver;

    public SubscriptionResolverTests() => _resolver = new SubscriptionResolver(_reader, _directory);

    [Fact]
    public async Task User_subscription_yields_direct_target_with_provenance()
    {
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        GivenSubscriptions(new SubscriptionMatch(subscriptionId, SubscriberType.User, userId, [Channel.InApp], null));

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: null);

        targets.Should().ContainSingle().Which.Should().Be(new ResolvedTarget(userId, Channel.InApp, subscriptionId));
    }

    [Fact]
    public async Task Role_subscription_expands_to_members_across_channels()
    {
        var roleId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        GivenSubscriptions(new SubscriptionMatch(Guid.NewGuid(), SubscriberType.Role, roleId, [Channel.InApp, Channel.Push], null));
        _directory.GetUsersInRoleAsync(roleId, Arg.Any<CancellationToken>()).Returns([member1, member2]);

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: null);

        targets.Select(target => (target.UserId, target.Channel)).Should().BeEquivalentTo(new[]
        {
            (member1, Channel.InApp),
            (member1, Channel.Push),
            (member2, Channel.InApp),
            (member2, Channel.Push),
        });
    }

    [Fact]
    public async Task Scoped_subscription_excluded_when_warehouse_differs()
    {
        var warehouse = Guid.NewGuid();
        var other = Guid.NewGuid();
        GivenSubscriptions(new SubscriptionMatch(Guid.NewGuid(), SubscriberType.User, Guid.NewGuid(), [Channel.InApp], warehouse));

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: other);

        targets.Should().BeEmpty();
    }

    [Fact]
    public async Task Scoped_subscription_included_when_warehouse_matches()
    {
        var warehouse = Guid.NewGuid();
        var userId = Guid.NewGuid();
        GivenSubscriptions(new SubscriptionMatch(Guid.NewGuid(), SubscriberType.User, userId, [Channel.InApp], warehouse));

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: warehouse);

        targets.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Null_scope_matches_any_warehouse()
    {
        var userId = Guid.NewGuid();
        GivenSubscriptions(new SubscriptionMatch(Guid.NewGuid(), SubscriberType.User, userId, [Channel.Email], null));

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: Guid.NewGuid());

        targets.Should().ContainSingle();
    }

    [Fact]
    public async Task Duplicate_user_channel_is_deduped()
    {
        // User punya subscription langsung, juga anggota Role yang subscribe event sama channel sama.
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        GivenSubscriptions(
            new SubscriptionMatch(Guid.NewGuid(), SubscriberType.User, userId, [Channel.InApp], null),
            new SubscriptionMatch(Guid.NewGuid(), SubscriberType.Role, roleId, [Channel.InApp], null));
        _directory.GetUsersInRoleAsync(roleId, Arg.Any<CancellationToken>()).Returns([userId]);

        var targets = await _resolver.ResolveAsync("WaveReady", warehouseId: null);

        targets.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }

    private void GivenSubscriptions(params SubscriptionMatch[] matches) =>
        _reader.ListForEventAsync("WaveReady", Arg.Any<CancellationToken>())
            .Returns(matches);
}

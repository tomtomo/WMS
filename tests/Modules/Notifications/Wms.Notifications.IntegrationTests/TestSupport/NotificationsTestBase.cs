using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests.TestSupport;

// Base integration test
public abstract class NotificationsTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    protected IInAppNotifier InAppNotifier { get; } = Substitute.For<IInAppNotifier>();

    protected IEmailSender EmailSender { get; } = Substitute.For<IEmailSender>();

    protected IPushNotifier PushNotifier { get; } = Substitute.For<IPushNotifier>();

    protected FakeUserDirectory Directory { get; } = new();

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = NotificationsTestHost.Build(connectionString, services =>
        {
            services.AddSingleton(InAppNotifier);
            services.AddSingleton(EmailSender);
            services.AddSingleton(PushNotifier);
            services.AddSingleton<IUserDirectory>(Directory);
        });
        await NotificationsTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    // Jalankan consumer dalam scope baru.
    protected async Task<Result> DeliverAsync<TConsumer>(Func<TConsumer, Task<Result>> consume)
        where TConsumer : notnull
    {
        ArgumentNullException.ThrowIfNull(consume);
        using var scope = _provider.CreateScope();
        return await consume(scope.ServiceProvider.GetRequiredService<TConsumer>());
    }

    // Proses semua delivery yang masih pending.
    protected Task<int> DispatchAsync() =>
        _provider.GetRequiredService<DeliveryDispatchRunner>().DispatchPendingAsync();

    protected async Task<TResult> ScopedAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var scope = _provider.CreateScope();
        return await action(scope.ServiceProvider);
    }

    protected async Task<TResult> QueryAsync<TResult>(Func<NotificationsDbContext, Task<TResult>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        using var scope = _provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<NotificationsDbContext>());
    }

    protected async Task SeedSubscriptionAsync(
        SubscriberType subscriberType,
        Guid subscriberId,
        string topic,
        Channel[] channels,
        Guid? warehouseScope = null,
        bool isActive = true)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var subscription = NotificationSubscription.Create(
            SubscriptionId.Create(Guid.NewGuid()).Value, subscriberType, subscriberId, topic, channels, warehouseScope).Value;
        if (!isActive)
        {
            subscription.Deactivate();
        }

        context.Set<NotificationSubscription>().Add(subscription);
        await context.SaveChangesAsync();
    }

    protected Task<List<NotificationDelivery>> AllDeliveriesAsync() =>
        QueryAsync(context => context.Set<NotificationDelivery>().AsNoTracking().ToListAsync());
}

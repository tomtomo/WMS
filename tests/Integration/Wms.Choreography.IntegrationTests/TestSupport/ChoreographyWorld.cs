using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Contracts.Abstractions;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Infrastructure;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Persistence;
using Wms.Outbound.Infrastructure;
using Wms.Platform.Local.Eventing;
using Wms.Platform.Local.Messaging;
using Wms.Reporting.Persistence;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Menyiapkan environment untuk test choreography antar modul.
internal sealed class ChoreographyWorld : IAsyncDisposable
{
    private readonly ServiceProvider[] _producers;

    private ChoreographyWorld(
        string exchange,
        FakeUserDirectory userDirectory,
        ServiceProvider inbound,
        ServiceProvider inventory,
        ServiceProvider outbound,
        ServiceProvider notifications,
        ServiceProvider reporting)
    {
        Exchange = exchange;
        UserDirectory = userDirectory;
        Inbound = inbound;
        Inventory = inventory;
        Outbound = outbound;
        Notifications = notifications;
        Reporting = reporting;
        _producers = [inbound, inventory, outbound];
    }

    public string Exchange { get; }

    public FakeUserDirectory UserDirectory { get; }

    public ServiceProvider Inbound { get; }

    public ServiceProvider Inventory { get; }

    public ServiceProvider Outbound { get; }

    public ServiceProvider Notifications { get; }

    public ServiceProvider Reporting { get; }

    public static async Task<ChoreographyWorld> CreateAsync(
        ChoreographyFixture fixture,
        Action<IServiceCollection>? customizeOutbound = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var token = Guid.NewGuid().ToString("N");
        var exchange = $"wms.events.{token}";
        var rabbit = fixture.RabbitMqConnectionString;
        var userDirectory = new FakeUserDirectory();

        var inbound = BuildInbound(await fixture.CreateFreshDatabaseAsync("inbound"), rabbit, exchange, $"wms.inbound.{token}");
        var inventory = BuildInventory(await fixture.CreateFreshDatabaseAsync("inventory"), rabbit, exchange, $"wms.inventory.{token}");
        var outbound = BuildOutbound(await fixture.CreateFreshDatabaseAsync("outbound"), rabbit, exchange, $"wms.outbound.{token}", customizeOutbound);
        var notifications = BuildNotifications(
            await fixture.CreateFreshDatabaseAsync("notifications"), rabbit, exchange, $"wms.notifications.{token}", userDirectory);
        var reporting = BuildReporting(await fixture.CreateFreshDatabaseAsync("reporting"), rabbit, exchange, $"wms.reporting.{token}");

        await MigrateAsync<InboundDbContext>(inbound);
        await MigrateAsync<InventoryDbContext>(inventory);
        await MigrateAsync<OutboundDbContext>(outbound);
        await MigrateAsync<NotificationsDbContext>(notifications);
        await MigrateAsync<ReportingDbContext>(reporting);

        var world = new ChoreographyWorld(exchange, userDirectory, inbound, inventory, outbound, notifications, reporting);

        // Bind queue consumer sebelum event dipublish
        await StartSubscriberAsync(inventory);
        await StartSubscriberAsync(outbound);
        await StartSubscriberAsync(notifications);
        await StartSubscriberAsync(reporting);

        return world;
    }

    // Dispatch command lewat MediatR
    public static async Task<Result> SendAsync(ServiceProvider module, IRequest<Result> command)
    {
        using var scope = module.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    public static async Task<Result<TValue>> SendAsync<TValue>(ServiceProvider module, IRequest<Result<TValue>> command)
    {
        using var scope = module.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    public static async Task<TResult> QueryAsync<TDbContext, TResult>(ServiceProvider module, Func<TDbContext, Task<TResult>> query)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(query);
        using var scope = module.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<TDbContext>());
    }

    public static async Task<TResult> ScopedAsync<TResult>(ServiceProvider module, Func<IServiceProvider, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var scope = module.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public static async Task<IReadOnlyList<OutboxRecord>> OutboxRowsAsync(ServiceProvider module, string? logicalName = null)
    {
        using var scope = module.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var query = dbContext.Set<OutboxRecord>().AsNoTracking().OrderBy(row => row.OccurredAt).AsQueryable();
        if (logicalName is not null)
        {
            query = query.Where(row => row.LogicalName == logicalName);
        }

        return await query.ToListAsync();
    }

    public static async Task<int> InboxCountAsync(ServiceProvider module, Guid eventId, string handlerType)
    {
        using var scope = module.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await dbContext.Set<InboxRecord>().AsNoTracking()
            .CountAsync(row => row.EventId == eventId && row.HandlerType == handlerType);
    }

    // Jumlah row Inbox per handlerType
    public static async Task<int> InboxHandlerCountAsync(ServiceProvider module, string handlerType)
    {
        using var scope = module.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await dbContext.Set<InboxRecord>().AsNoTracking().CountAsync(row => row.HandlerType == handlerType);
    }

    // Rekonstruksi envelope dari row Outbox
    public static async Task<MessageEnvelope> EnvelopeAsync(
        ServiceProvider module, string logicalName, DeliveryClass deliveryClass = DeliveryClass.CoreFlow)
    {
        var rows = await OutboxRowsAsync(module, logicalName);
        var row = rows.First(candidate => candidate.DeliveryClass == deliveryClass);
        return new MessageEnvelope(
            row.Id, row.LogicalName, row.DeliveryClass, row.OccurredAt, row.Payload, row.Traceparent, row.Tracestate);
    }

    // Publish envelope apa adanya ke broker
    public static Task PublishAsync(ServiceProvider module, MessageEnvelope envelope) =>
        module.GetRequiredService<IMessagePublisher>().PublishAsync(envelope);

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        if (!await condition().ConfigureAwait(false))
        {
            throw new TimeoutException($"Kondisi tak terpenuhi dalam {timeout.TotalSeconds}s.");
        }
    }

    // Jalankan siklus dispatch sampai kondisi terpenuhi.
    public async Task PumpUntilAsync(Func<Task<bool>> done, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(done);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await DrainProducersAsync();
            if (await done().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        await DrainProducersAsync();
        if (!await done().ConfigureAwait(false))
        {
            throw new TimeoutException($"Choreography tak mencapai kondisi terminal dalam {timeout.TotalSeconds}s.");
        }
    }

    // Publish satu siklus outbox tiap producer
    public async Task DrainProducersAsync()
    {
        foreach (var producer in _producers)
        {
            var worker = producer.GetRequiredService<OutboxDispatcherWorker>();
            await worker.DrainOnceAsync(CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Inbound.DisposeAsync();
        await Inventory.DisposeAsync();
        await Outbound.DisposeAsync();
        await Notifications.DisposeAsync();
        await Reporting.DisposeAsync();
    }

    private static async Task MigrateAsync<TDbContext>(IServiceProvider provider)
        where TDbContext : DbContext
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TDbContext>().Database.MigrateAsync();
    }

    private static async Task StartSubscriberAsync(IServiceProvider provider)
    {
        var worker = provider.GetRequiredService<RailSubscriberWorker>();
        await worker.SubscribeOnceAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildInbound(string wms, string rabbit, string exchange, string queue)
    {
        var configuration = BuildConfiguration(wms, rabbit);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(Wms.Inbound.Application.Features.ConfirmGoodsReceipt.ConfirmGoodsReceiptCommand).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-inbound");
        services.AddInboundModule(configuration);
        services.AddSingleton<Wms.Inbound.Application.Abstractions.IProductReader, FakeProductReader>();
        services.AddSingleton<Wms.Inbound.Application.Abstractions.IWarehouseReader, FakeInboundWarehouseReader>();
        AddRailTransport(services, configuration, exchange);
        services.AddEventingRail(queue);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ServiceProvider BuildInventory(string wms, string rabbit, string exchange, string queue)
    {
        var configuration = BuildConfiguration(wms, rabbit);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(Wms.Inventory.Application.Features.CompletePutaway.CompletePutawayCommand).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-inventory");
        services.AddInventoryModule(configuration);
        services.AddSingleton<Wms.Inventory.Application.Abstractions.IReceivingPolicy, FakeReceivingPolicy>();
        AddRailTransport(services, configuration, exchange);
        services.AddInventoryRailConsumers();
        services.AddEventingRail(queue);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ServiceProvider BuildOutbound(
        string wms, string rabbit, string exchange, string queue, Action<IServiceCollection>? customize)
    {
        var configuration = BuildConfiguration(wms, rabbit);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(Wms.Outbound.Application.Features.CreateWave.CreateWaveCommand).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-outbound");
        services.AddOutboundModule(configuration);
        services.AddSingleton<Wms.Outbound.Application.Abstractions.IWarehouseReader, FakeOutboundWarehouseReader>();
        services.AddSingleton<Wms.Outbound.Application.Abstractions.IPickAssignmentPolicy, FakePickAssignmentPolicy>();
        AddRailTransport(services, configuration, exchange);
        services.AddOutboundRailConsumers();
        services.AddEventingRail(queue);

        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ServiceProvider BuildNotifications(string wms, string rabbit, string exchange, string queue, IUserDirectory userDirectory)
    {
        var configuration = BuildConfiguration(wms, rabbit);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(NotificationsDbContext).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-notifications");
        services.AddNotificationsModule(configuration);
        services.AddSingleton(userDirectory);
        AddRailTransport(services, configuration, exchange);
        services.AddNotificationsRailConsumers();
        services.AddEventingRail(queue);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ServiceProvider BuildReporting(string wms, string rabbit, string exchange, string queue)
    {
        var configuration = BuildConfiguration(wms, rabbit);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-reporting");
        services.AddReportingModule(configuration);
        AddRailTransport(services, configuration, exchange);
        services.AddReportingRailConsumers();
        services.AddEventingRail(queue);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void AddRailTransport(IServiceCollection services, IConfiguration configuration, string exchange)
    {
        services.AddSingleton(configuration);
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ExchangeName = exchange;
            options.ConnectionStringName = "rabbitmq";
        });
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IMessageSubscriber, RabbitMqMessageSubscriber>();
        services.AddSingleton<OutboxDispatcher, RabbitMqOutboxDispatcher>();
    }

    private static IConfiguration BuildConfiguration(string wmsConnectionString, string rabbitConnectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = wmsConnectionString,
                ["ConnectionStrings:rabbitmq"] = rabbitConnectionString,
            })
            .Build();
}

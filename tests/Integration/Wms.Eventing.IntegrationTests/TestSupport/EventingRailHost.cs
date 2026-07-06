using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Inbound.Infrastructure;
using Wms.Platform.Local.Eventing;
using Wms.Platform.Local.Messaging;
using Wms.Reporting.Persistence;

namespace Wms.Eventing.IntegrationTests.TestSupport;

// Helper untuk membuat host test eventing rail.
internal static class EventingRailHost
{
    // Buat host producer untuk modul Inbound.
    public static ServiceProvider BuildInboundProducer(string wmsConnectionString, string rabbitConnectionString, string exchange)
    {
        var configuration = BuildConfiguration(wmsConnectionString, rabbitConnectionString);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, SystemCurrentUser>();
        services.AddBuildingBlocksInfrastructure("wms-inbound-producer");
        services.AddInboundModule(configuration);
        AddRailTransport(services, configuration, exchange);
        services.AddEventingRail("wms.inbound-producer");
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    // Consumer Reporting
    public static ServiceProvider BuildReportingConsumer(string wmsConnectionString, string rabbitConnectionString, string exchange, string queueName)
    {
        var configuration = BuildConfiguration(wmsConnectionString, rabbitConnectionString);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, SystemCurrentUser>();
        services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-reporting-consumer");
        services.AddReportingModule(configuration);
        services.AddReportingRailConsumers();
        AddRailTransport(services, configuration, exchange);
        services.AddEventingRail(queueName);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task MigrateAsync<TDbContext>(IServiceProvider provider)
        where TDbContext : DbContext
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TDbContext>().Database.MigrateAsync();
    }

    public static async Task StartSubscriberAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var worker = provider.GetRequiredService<RailSubscriberWorker>();
        await worker.SubscribeOnceAsync(cancellationToken);
    }

    public static async Task EmitToOutboxAsync<TIntegrationEvent>(
        IServiceProvider provider,
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        await outbox.AddAsync(integrationEvent, deliveryClass, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<OutboxRecord>> OutboxRowsAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await dbContext.Set<OutboxRecord>().AsNoTracking().OrderBy(row => row.OccurredAt).ToListAsync(cancellationToken);
    }

    // Satu siklus dispatch tanpa menunggu interval 5s.
    public static async Task DrainAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var worker = provider.GetRequiredService<OutboxDispatcherWorker>();
        await worker.DrainOnceAsync(cancellationToken);
    }

    public static async Task<int> InboxCountAsync(IServiceProvider provider, Guid eventId, CancellationToken cancellationToken = default)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await dbContext.Set<InboxRecord>().AsNoTracking().CountAsync(row => row.EventId == eventId, cancellationToken);
    }

    // Poll kondisi sampai true atau timeout
    public static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Kondisi tak terpenuhi dalam {timeout.TotalSeconds}s.");
    }

    private static void AddRailTransport(IServiceCollection services, IConfiguration configuration, string exchange)
    {
        // RabbitMqConnectionFactory resolve IConfiguration untuk connection string broker.
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

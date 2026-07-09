using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Contracts.Abstractions;
using Wms.Platform.Local.Eventing;
using Wms.Platform.Local.Messaging;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Helper untuk membuat host test dead letter.
internal static class DeadLetterHost
{
    public static async Task<ServiceProvider> StartConsumerAsync(
        string wmsConnectionString,
        string rabbitConnectionString,
        string exchange,
        string queue,
        FaultInjector fault,
        string logicalName,
        DeliveryClass deliveryClass)
    {
        var services = BaseServices(wmsConnectionString, rabbitConnectionString, exchange);
        services.AddSingleton(new RailConsumerRegistration
        {
            LogicalName = logicalName,
            DeliveryClass = deliveryClass,
            InvokeAsync = fault.InvokeAsync,
        });
        services.AddEventingRail(queue);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await EnsureCreatedAsync(provider);
        await provider.GetRequiredService<RailSubscriberWorker>().SubscribeOnceAsync(CancellationToken.None);
        return provider;
    }

    // Menyiapkan dua consumer yang menangani event yang sama.
    public static async Task<ServiceProvider> StartFanOutConsumerAsync(
        string wmsConnectionString,
        string rabbitConnectionString,
        string exchange,
        string queue,
        FaultInjector first,
        FaultInjector second,
        string logicalName,
        DeliveryClass deliveryClass)
    {
        var services = BaseServices(wmsConnectionString, rabbitConnectionString, exchange);
        services.AddSingleton(new RailConsumerRegistration
        {
            LogicalName = logicalName,
            DeliveryClass = deliveryClass,
            InvokeAsync = first.InvokeAsync,
        });
        services.AddSingleton(new RailConsumerRegistration
        {
            LogicalName = logicalName,
            DeliveryClass = deliveryClass,
            InvokeAsync = second.InvokeAsync,
        });
        services.AddEventingRail(queue);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await EnsureCreatedAsync(provider);
        await provider.GetRequiredService<RailSubscriberWorker>().SubscribeOnceAsync(CancellationToken.None);
        return provider;
    }

    public static async Task<ServiceProvider> BuildProducerAsync(
        string wmsConnectionString, string rabbitConnectionString, string exchange, string queue)
    {
        var services = BaseServices(wmsConnectionString, rabbitConnectionString, exchange);
        services.AddEventingRail(queue);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await EnsureCreatedAsync(provider);
        return provider;
    }

    public static Task PublishAsync(
        IServiceProvider host, string logicalName, DeliveryClass deliveryClass, string payload = "{}")
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(), logicalName, deliveryClass, DateTimeOffset.UtcNow, payload, null, null);
        return host.GetRequiredService<IMessagePublisher>().PublishAsync(envelope);
    }

    public static async Task EmitAsync<TEvent>(IServiceProvider producer, TEvent integrationEvent, DeliveryClass deliveryClass)
        where TEvent : IIntegrationEvent
    {
        using var scope = producer.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        await outbox.AddAsync(integrationEvent, deliveryClass);
        await dbContext.SaveChangesAsync();
    }

    public static Task DrainAsync(IServiceProvider producer) =>
        producer.GetRequiredService<OutboxDispatcherWorker>().DrainOnceAsync(CancellationToken.None);

    public static async Task<IReadOnlyList<OutboxRecord>> OutboxRowsAsync(IServiceProvider producer)
    {
        using var scope = producer.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await dbContext.Set<OutboxRecord>().AsNoTracking().ToListAsync();
    }

    public static async Task<IReadOnlyList<DeadLetterRecord>> DeadLettersAsync(IServiceProvider host)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RailInfraDbContext>();
        return await dbContext.Set<DeadLetterRecord>().AsNoTracking().ToListAsync();
    }

    public static async Task<int> DeadLetterCountAsync(IServiceProvider host)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RailInfraDbContext>();
        return await dbContext.Set<DeadLetterRecord>().AsNoTracking().CountAsync();
    }

    private static IServiceCollection BaseServices(string wmsConnectionString, string rabbitConnectionString, string exchange)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = wmsConnectionString,
                ["ConnectionStrings:rabbitmq"] = rabbitConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentUser, TestCurrentUser>();
        services.AddDbContext<RailInfraDbContext>(options => options.UseNpgsql(wmsConnectionString));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<RailInfraDbContext>());
        services.AddBuildingBlocksInfrastructure("wms-dlq-tests");
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ExchangeName = exchange;
            options.ConnectionStringName = "rabbitmq";
        });
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IMessageSubscriber, RabbitMqMessageSubscriber>();
        services.AddSingleton<OutboxDispatcher, RabbitMqOutboxDispatcher>();
        return services;
    }

    private static async Task EnsureCreatedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RailInfraDbContext>().Database.EnsureCreatedAsync();
    }
}

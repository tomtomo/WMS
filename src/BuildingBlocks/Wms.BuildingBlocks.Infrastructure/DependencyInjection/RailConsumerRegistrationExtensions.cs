using System.Text.Json;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Wms.Contracts.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

// Mapping integration event ke consumer rail modul.
public static class RailConsumerRegistrationExtensions
{
    public static IServiceCollection AddRailConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        DeliveryClass deliveryClass,
        Func<TConsumer, TEvent, MessageEnvelope, CancellationToken, Task<Result>> consume)
        where TEvent : IIntegrationEvent
        where TConsumer : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(consume);

        var logicalName = IntegrationEventLogicalName.Resolve(typeof(TEvent));
        services.AddSingleton(new RailConsumerRegistration
        {
            LogicalName = logicalName,
            DeliveryClass = deliveryClass,
            InvokeAsync = async (provider, envelope, cancellationToken) =>
            {
                var payload = JsonSerializer.Deserialize<TEvent>(envelope.Payload, MessageEnvelope.PayloadSerializerOptions)
                    ?? throw new JsonException($"Payload '{logicalName}' tidak bisa dibaca.");
                var consumer = provider.GetRequiredService<TConsumer>();
                return await consume(consumer, payload, envelope, cancellationToken).ConfigureAwait(false);
            },
        });

        return services;
    }
}

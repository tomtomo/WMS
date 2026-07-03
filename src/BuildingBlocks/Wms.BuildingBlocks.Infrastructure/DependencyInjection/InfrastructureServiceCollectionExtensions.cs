using Microsoft.Extensions.DependencyInjection.Extensions;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.BuildingBlocks.Infrastructure.Telemetry;

namespace Microsoft.Extensions.DependencyInjection;

// Composition root mekanisme infra. Rail/interceptor/outbox = scoped.
// Resilience dipasang per HttpClient lewat AddHttpResilience/AddGrpcResilience di host, bukan service global.
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInboxGuard, InboxGuard>();
        services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
        services.AddScoped<IDeadLetterStore, DeadLetterStore>();
        services.AddScoped<ConsumerDeadLetterPipeline>();
        services.AddScoped<AuditableInterceptor>();

        // Buka DI scope sendiri. Tidak boleh menahan DbContext scoped, jadi singleton.
        services.AddSingleton<IAuditLogStore, AuditLogStore>();

        services.AddInfrastructureTelemetry(serviceName);

        return services;
    }
}

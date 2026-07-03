using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;
using Wms.BuildingBlocks.Application;
using Wms.BuildingBlocks.Application.Behaviors;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi kernel Application untuk host: MediatR, 4 pipeline behavior dengan urutan Validation lalu Transaction lalu AuditLog lalu Logging, FluentValidation, feature management.
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationBuildingBlocks(
        this IServiceCollection services,
        params Assembly[] moduleAssemblies)
    {
        var scanAssemblies = moduleAssemblies.Length > 0
            ? moduleAssemblies
            : [typeof(IApplicationBuildingBlocksMarker).Assembly];

        // Urutan AddOpenBehavior menentukan urutan masuk pipeline.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(scanAssemblies);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
            configuration.AddOpenBehavior(typeof(AuditLogBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(scanAssemblies);
        services.AddFeatureManagement();

        // Clock untuk AuditLog/Logging.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}

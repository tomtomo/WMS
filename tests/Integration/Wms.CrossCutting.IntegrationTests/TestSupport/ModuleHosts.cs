using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Infrastructure;
using Wms.MasterData.Infrastructure;
using Wms.Notifications.Persistence;
using Wms.Outbound.Infrastructure;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Komposisi host per modul untuk sweep cross cutting, pipeline penuh tanpa rail broker.
internal static class ModuleHosts
{
    public static async Task<ServiceProvider> BuildInboundAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-inbound",
            typeof(Wms.Inbound.Application.Features.CreateGoodsReceiptHeader.CreateGoodsReceiptHeaderCommand).Assembly);
        services.AddInboundModule(Configuration(connectionString));
        services.AddSingleton<Wms.Inbound.Application.Abstractions.IProductReader, FakeProductReader>();
        services.AddSingleton<Wms.Inbound.Application.Abstractions.IWarehouseReader, FakeInboundWarehouseReader>();
        var provider = Finish(services, customize);
        await MigrateAsync<InboundDbContext>(provider);
        return provider;
    }

    public static async Task<ServiceProvider> BuildInventoryAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-inventory",
            typeof(Wms.Inventory.Application.Features.DetectNearExpiry.DetectNearExpiryCommand).Assembly);
        services.AddInventoryModule(Configuration(connectionString));
        services.AddSingleton<Wms.Inventory.Application.Abstractions.IReceivingPolicy, FakeReceivingPolicy>();
        var provider = Finish(services, customize);
        await MigrateAsync<InventoryDbContext>(provider);
        return provider;
    }

    public static async Task<ServiceProvider> BuildOutboundAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-outbound",
            typeof(Wms.Outbound.Application.Features.CreateWave.CreateWaveCommand).Assembly);
        services.AddOutboundModule(Configuration(connectionString));
        services.AddSingleton<Wms.Outbound.Application.Abstractions.IWarehouseReader, FakeOutboundWarehouseReader>();
        services.AddSingleton<Wms.Outbound.Application.Abstractions.IPickAssignmentPolicy, FakePickAssignmentPolicy>();
        var provider = Finish(services, customize);
        await MigrateAsync<OutboundDbContext>(provider);
        return provider;
    }

    public static async Task<ServiceProvider> BuildMasterDataAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-masterdata",
            typeof(Wms.MasterData.Application.MasterDataPermissions).Assembly);
        services.AddMasterDataModule(Configuration(connectionString));
        services.AddSingleton<ICacheStore, Wms.Platform.Local.Cache.InMemoryCacheStore>();
        var provider = Finish(services, customize);
        await MigrateAsync<MasterDataDbContext>(provider);
        return provider;
    }

    public static async Task<ServiceProvider> BuildAuthAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-auth",
            typeof(Wms.Auth.Application.AuthPermissions).Assembly);
        services.AddAuthModule(Configuration(connectionString));
        var provider = Finish(services, customize);
        await MigrateAsync<Wms.Auth.Infrastructure.AuthDbContext>(provider);
        return provider;
    }

    public static async Task<ServiceProvider> BuildNotificationsAsync(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = CoreServices(
            "wms-cc-notifications",
            typeof(NotificationsDbContext).Assembly);
        services.AddNotificationsModule(Configuration(connectionString));
        services.AddSingleton<Wms.Notifications.Abstractions.IUserDirectory, FakeUserDirectory>();
        var provider = Finish(services, customize);
        await MigrateAsync<NotificationsDbContext>(provider);
        return provider;
    }

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

    public static async Task<IReadOnlyList<AuditLogRecord>> AuditLogRowsAsync(ServiceProvider module)
    {
        using var scope = module.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DbContext>();
        return await context.Set<AuditLogRecord>().AsNoTracking().OrderBy(row => row.OccurredAt).ToListAsync();
    }

    public static async Task<TResult> QueryAsync<TDbContext, TResult>(ServiceProvider module, Func<TDbContext, Task<TResult>> query)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(query);
        using var scope = module.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<TDbContext>());
    }

    private static ServiceCollection CoreServices(string serviceName, System.Reflection.Assembly moduleAssembly)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationBuildingBlocks(moduleAssembly);
        services.AddBuildingBlocksInfrastructure(serviceName);

        // Injector konflik selalu terpasang supaya komposisi host seragam.
        services.AddSingleton<ConflictInjector>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConflictInjectionBehavior<,>));
        return services;
    }

    private static ServiceProvider Finish(ServiceCollection services, Action<IServiceCollection>? customize)
    {
        customize?.Invoke(services);

        // Default aktor test
        services.TryAddSingletonCurrentUser();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void TryAddSingletonCurrentUser(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(ICurrentUser)))
        {
            services.AddSingleton<ICurrentUser>(new FixedCurrentUser());
        }
    }

    private static IConfiguration Configuration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

    private static async Task MigrateAsync<TDbContext>(IServiceProvider provider)
        where TDbContext : DbContext
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TDbContext>().Database.MigrateAsync();
    }
}

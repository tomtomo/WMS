using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.Features.ReceiveGoodsReceipt;
using Wms.Inventory.Infrastructure;

namespace Wms.Inventory.IntegrationTests.TestSupport;

internal static class InventoryTestHost
{
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        AddInventoryComposition(services, connectionString);
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static void AddInventoryComposition(IServiceCollection services, string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(GRConfirmedConsumer).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-inventory-tests");
        services.AddInventoryModule(configuration);

        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());

        services.AddSingleton<IReceivingPolicy, FakeReceivingPolicy>();

        // Gunakan publisher test untuk merekam telemetry operasional tanpa melibatkan adapter platform.
        services.AddSingleton<IEventStreamPublisher, CapturingEventStreamPublisher>();
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
    }
}

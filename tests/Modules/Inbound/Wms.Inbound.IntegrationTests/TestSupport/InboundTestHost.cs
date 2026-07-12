using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;
using Wms.Inbound.Infrastructure;

namespace Wms.Inbound.IntegrationTests.TestSupport;

internal static class InboundTestHost
{
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        AddInboundComposition(services, connectionString);
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static void AddInboundComposition(IServiceCollection services, string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(GoodsReceiptEventTranslator).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-inbound-tests");
        services.AddInboundModule(configuration);

        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());
        services.AddSingleton<IObjectStore>(new InMemoryObjectStore());
        services.AddSingleton<IProductReader>(new FakeProductReader());
        services.AddSingleton<IWarehouseReader>(new FakeWarehouseReader());

        // Gunakan publisher test untuk merekam telemetry operasional tanpa melibatkan adapter platform.
        services.AddSingleton<IEventStreamPublisher, CapturingEventStreamPublisher>();
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InboundDbContext>().Database.MigrateAsync();
    }
}

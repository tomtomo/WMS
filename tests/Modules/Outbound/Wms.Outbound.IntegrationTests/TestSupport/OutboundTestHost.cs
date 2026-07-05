using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Infrastructure;

namespace Wms.Outbound.IntegrationTests.TestSupport;

internal static class OutboundTestHost
{
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        AddOutboundComposition(services, connectionString);
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static void AddOutboundComposition(IServiceCollection services, string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(IWaveRepository).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-outbound-tests");
        services.AddOutboundModule(configuration);

        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());

        // Master Data ACL — fake
        services.AddSingleton<IWarehouseReader>(new FakeWarehouseReader());

        // Assignment picker — fake
        services.AddSingleton<IPickAssignmentPolicy>(new FakePickAssignmentPolicy());
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OutboundDbContext>().Database.MigrateAsync();
    }
}

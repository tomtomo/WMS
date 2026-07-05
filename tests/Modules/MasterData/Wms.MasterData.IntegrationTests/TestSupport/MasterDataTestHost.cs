using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Application;
using Wms.MasterData.Infrastructure;
using Wms.Platform.Local.Cache;

namespace Wms.MasterData.IntegrationTests.TestSupport;

// Komposisi modul MasterData untuk test
internal static class MasterDataTestHost
{
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        AddMasterDataComposition(services, connectionString);
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static void AddMasterDataComposition(IServiceCollection services, string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(MasterDataPermissions).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-masterdata-tests");
        services.AddMasterDataModule(configuration);

        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());

        // Adapter cache aside Local (cloud: Redis Azure / Memorystore GCP).
        services.AddSingleton<ICacheStore, InMemoryCacheStore>();
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MasterDataDbContext>().Database.MigrateAsync();
    }
}

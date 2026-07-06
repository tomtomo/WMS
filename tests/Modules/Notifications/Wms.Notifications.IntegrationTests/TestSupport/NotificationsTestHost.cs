using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Notifications.Persistence;

namespace Wms.Notifications.IntegrationTests.TestSupport;

// Komposisi modul Notifications untuk test
internal static class NotificationsTestHost
{
    public static ServiceProvider Build(string connectionString, Action<IServiceCollection> configureChannels)
    {
        ArgumentNullException.ThrowIfNull(configureChannels);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(NotificationsDbContext).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-notifications-tests");
        services.AddNotificationsModule(configuration);

        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());

        // Adapter channel
        configureChannels(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }
}

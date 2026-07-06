using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Reporting.Persistence;

namespace Wms.Reporting.IntegrationTests.TestSupport;

// Komposisi modul Reporting untuk test
internal static class ReportingTestHost
{
    public static ServiceProvider Build(string connectionString)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:wms"] = connectionString,
            })
            .Build();

        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-reporting-tests");
        services.AddReportingModule(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ReportingDbContext>().Database.MigrateAsync();
    }
}

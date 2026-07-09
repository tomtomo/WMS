using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Domain;
using Wms.MasterData.Infrastructure;
using Wms.Migration.IntegrationTests.TestSupport;
using Wms.MigrationRunner;
using Wms.Platform.Local.Security;
using Xunit;

namespace Wms.Migration.IntegrationTests;

// MigrationRunner DB per module
[Collection(MigrationCollection.Name)]
public sealed class DbPerModuleMigrationTests(MigrationFixture fixture)
{
    [Fact]
    public async Task All_modules_migrate_to_per_module_databases_and_seed_is_idempotent()
    {
        var connectionStrings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{MigrationModules.Inbound}"] = await fixture.CreateFreshDatabaseAsync("inbound"),
            [$"ConnectionStrings:{MigrationModules.Inventory}"] = await fixture.CreateFreshDatabaseAsync("inventory"),
            [$"ConnectionStrings:{MigrationModules.Outbound}"] = await fixture.CreateFreshDatabaseAsync("outbound"),
            [$"ConnectionStrings:{MigrationModules.MasterData}"] = await fixture.CreateFreshDatabaseAsync("masterdata"),
            [$"ConnectionStrings:{MigrationModules.Auth}"] = await fixture.CreateFreshDatabaseAsync("auth"),
            [$"ConnectionStrings:{MigrationModules.Reporting}"] = await fixture.CreateFreshDatabaseAsync("reporting"),
            [$"ConnectionStrings:{MigrationModules.Notifications}"] = await fixture.CreateFreshDatabaseAsync("notifications"),
        };

        await using var provider = BuildProvider(connectionStrings);
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        // Jalankan migration untuk semua modul. Tiap modul memakai database sendiri, jadi tabel infrastructure.* tidak saling bentrok.
        foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
        {
            await resolveDbContext(services).Database.MigrateAsync();
        }

        // Pastikan semua migration sudah diterapkan.
        foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
        {
            (await resolveDbContext(services).Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }

        // Seed dijalankan ulang untuk memastikan prosesnya idempotent,  yaitu data tidak bertambah dobel.
        foreach (var seed in ModuleMigratorRegistry.ModuleSeeders)
        {
            await seed(services, CancellationToken.None);
        }

        foreach (var seed in ModuleMigratorRegistry.ModuleSeeders)
        {
            await seed(services, CancellationToken.None);
        }

        var masterData = services.GetRequiredService<MasterDataDbContext>();
        (await masterData.Set<Warehouse>().IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await masterData.Set<Location>().IgnoreQueryFilters().CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task All_modules_sharing_one_database_collide_on_duplicate_infrastructure_table()
    {
        // Membuktikan kenapa tiap modul butuh database sendiri: kalau semua modul memakai database yang sama, tabel infrastructure.* akan bentrok.
        var shared = await fixture.CreateFreshDatabaseAsync("shared");
        var connectionStrings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{MigrationModules.Inbound}"] = shared,
            [$"ConnectionStrings:{MigrationModules.Inventory}"] = shared,
            [$"ConnectionStrings:{MigrationModules.Outbound}"] = shared,
            [$"ConnectionStrings:{MigrationModules.MasterData}"] = shared,
            [$"ConnectionStrings:{MigrationModules.Auth}"] = shared,
            [$"ConnectionStrings:{MigrationModules.Reporting}"] = shared,
            [$"ConnectionStrings:{MigrationModules.Notifications}"] = shared,
        };

        await using var provider = BuildProvider(connectionStrings);
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        Func<Task> migrateAll = async () =>
        {
            foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
            {
                await resolveDbContext(services).Database.MigrateAsync();
            }
        };

        (await migrateAll.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be(PostgresErrorCodes.DuplicateTable);
    }

    private static ServiceProvider BuildProvider(IDictionary<string, string?> connectionStrings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(connectionStrings).Build();
        var services = new ServiceCollection();
        services.AddAllModules(configuration);
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}

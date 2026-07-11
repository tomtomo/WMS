using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Domain;
using Wms.MasterData.Infrastructure;
using Wms.Migration.IntegrationTests.TestSupport;
using Wms.MigrationRunner;
using Wms.Platform.Azure.Persistence;
using Wms.Platform.Shared.Security;
using Xunit;

namespace Wms.Migration.IntegrationTests;

// Pastikan perpindahan ke Flexible Server hanya mengubah connection string tanpa memengaruhi migration, skema, atau xmin.
// Testcontainers digunakan untuk memverifikasi connection string adapter Azure dengan protokol PostgreSQL yang sama.
[Collection(MigrationCollection.Name)]
public sealed class FlexibleServerMigrationParityTests(MigrationFixture fixture)
{
    [Fact]
    public async Task All_modules_migrate_through_the_azure_composed_connection_string()
    {
        var connectionStrings = await ComposeAzureConnectionStringsAsync();
        await using var provider = BuildProvider(connectionStrings);
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
        {
            await resolveDbContext(services).Database.MigrateAsync();
        }

        foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
        {
            (await resolveDbContext(services).Database.GetPendingMigrationsAsync())
                .Should().BeEmpty("skema modul tak berubah saat store pindah ke Flexible Server");
        }
    }

    [Fact]
    public async Task Crud_and_the_xmin_concurrency_token_behave_exactly_as_on_local_postgres()
    {
        var connectionStrings = await ComposeAzureConnectionStringsAsync();
        await using var provider = BuildProvider(connectionStrings);
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        // Seeder dipakai beberapa modul, jadi seluruh skema disiapkan lebih dulu.
        foreach (var resolveDbContext in ModuleMigratorRegistry.ModuleDbContexts)
        {
            await resolveDbContext(services).Database.MigrateAsync();
        }

        foreach (var seed in ModuleMigratorRegistry.ModuleSeeders)
        {
            await seed(services, CancellationToken.None);
        }

        var masterData = services.GetRequiredService<MasterDataDbContext>();

        var warehouse = await masterData.Set<Warehouse>().IgnoreQueryFilters().SingleAsync();
        warehouse.Update(warehouse.Name + " (Flexible)", warehouse.Address).IsSuccess.Should().BeTrue();
        await masterData.SaveChangesAsync();

        masterData.ChangeTracker.Clear();
        (await masterData.Set<Warehouse>().IgnoreQueryFilters().SingleAsync()).Name.Should().EndWith("(Flexible)");

        // xmin tetap menjadi concurrency token bawaan Postgres, dan tidak ada kolom versi tambahan yang dibuat khusus untuk cloud.
        masterData.Model.FindEntityType(typeof(Warehouse))!
            .GetProperties()
            .Should().ContainSingle(property => property.IsConcurrencyToken && property.Name == "xmin");
    }

    private static ServiceProvider BuildProvider(IDictionary<string, string?> connectionStrings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(connectionStrings).Build();
        var services = new ServiceCollection();
        services.AddAllModules(configuration);
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    // Connection string tiap modul dibuat lewat adapter Azure, TLS dimatikan karena Postgres lokal tidak memakai TLS.
    private async Task<Dictionary<string, string?>> ComposeAzureConnectionStringsAsync()
    {
        string[] moduleConnectionNames =
        [
            MigrationModules.Inbound,
            MigrationModules.Inventory,
            MigrationModules.Outbound,
            MigrationModules.MasterData,
            MigrationModules.Auth,
            MigrationModules.Reporting,
            MigrationModules.Notifications,
        ];

        var rawConnectionStrings = new Dictionary<string, string?>();
        foreach (var name in moduleConnectionNames)
        {
            rawConnectionStrings[$"ConnectionStrings:{name}"] = await fixture.CreateFreshDatabaseAsync(name);
        }

        var rawConfiguration = new ConfigurationBuilder().AddInMemoryCollection(rawConnectionStrings).Build();
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider
            .GetSecretAsync("flexible-server-password", Arg.Any<CancellationToken>())
            .Returns(new NpgsqlConnectionStringBuilder(rawConnectionStrings[$"ConnectionStrings:{MigrationModules.Inbound}"]).Password);

        var composed = new Dictionary<string, string?>();
        foreach (var name in moduleConnectionNames)
        {
            var factory = new FlexibleServerConnectionStringFactory(
                rawConfiguration,
                Options.Create(new FlexibleServerOptions
                {
                    ConnectionStringName = name,
                    PasswordSecretName = "flexible-server-password",
                    RequireSsl = false,
                }),
                secretProvider);
            composed[$"ConnectionStrings:{name}"] = await factory.CreateAsync();
        }

        return composed;
    }
}
